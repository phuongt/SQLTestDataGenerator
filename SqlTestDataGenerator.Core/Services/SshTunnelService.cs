using Renci.SshNet;
using Serilog;
using System;
using System.Net;
using System.Threading.Tasks;

namespace SqlTestDataGenerator.Core.Services
{
    /// <summary>
    /// Service for creating SSH tunnels to connect to databases in private networks
    /// Supports password and key-based authentication
    /// </summary>
    public class SshTunnelService : IDisposable
    {
        private readonly ILogger _logger;
        private SshClient? _sshClient;
        private ForwardedPortLocal? _forwardedPort;
        private bool _disposed = false;

        public SshTunnelService()
        {
            _logger = Log.ForContext<SshTunnelService>();
        }

        /// <summary>
        /// Connection status of SSH tunnel
        /// </summary>
        public bool IsConnected => _sshClient?.IsConnected == true && _forwardedPort?.IsStarted == true;

        /// <summary>
        /// Local port used for tunnel (for connection string)
        /// </summary>
        public uint LocalPort => _forwardedPort?.BoundPort ?? 0;

        /// <summary>
        /// Create SSH tunnel with password authentication
        /// </summary>
        public async Task<bool> CreateTunnelAsync(
            string sshHost,
            int sshPort,
            string sshUsername,
            string sshPassword,
            string remoteDbHost,
            int remoteDbPort,
            uint localPort = 0)
        {
            try
            {
                _logger.Information("Creating SSH tunnel with password auth to {SshHost}:{SshPort}", sshHost, sshPort);
                Console.WriteLine($"[SshTunnelService] Creating SSH tunnel: {sshUsername}@{sshHost}:{sshPort} -> {remoteDbHost}:{remoteDbPort}");

                // Create SSH connection info with password
                var connectionInfo = new ConnectionInfo(sshHost, sshPort, sshUsername, new PasswordAuthenticationMethod(sshUsername, sshPassword))
                {
                    Timeout = TimeSpan.FromSeconds(30)
                };

                return await CreateTunnelInternalAsync(connectionInfo, remoteDbHost, remoteDbPort, localPort);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to create SSH tunnel with password auth");
                Console.WriteLine($"[SshTunnelService] SSH tunnel creation failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Create SSH tunnel with private key authentication
        /// </summary>
        public async Task<bool> CreateTunnelAsync(
            string sshHost,
            int sshPort,
            string sshUsername,
            string privateKeyPath,
            string? keyPassphrase,
            string remoteDbHost,
            int remoteDbPort,
            uint localPort = 0)
        {
            try
            {
                _logger.Information("Creating SSH tunnel with key auth to {SshHost}:{SshPort}", sshHost, sshPort);
                Console.WriteLine($"[SshTunnelService] Creating SSH tunnel with key: {sshUsername}@{sshHost}:{sshPort} -> {remoteDbHost}:{remoteDbPort}");

                // Create private key auth method
                AuthenticationMethod authMethod;
                if (string.IsNullOrEmpty(keyPassphrase))
                {
                    authMethod = new PrivateKeyAuthenticationMethod(sshUsername, new PrivateKeyFile(privateKeyPath));
                }
                else
                {
                    authMethod = new PrivateKeyAuthenticationMethod(sshUsername, new PrivateKeyFile(privateKeyPath, keyPassphrase));
                }

                // Create SSH connection info with key
                var connectionInfo = new ConnectionInfo(sshHost, sshPort, sshUsername, authMethod)
                {
                    Timeout = TimeSpan.FromSeconds(30)
                };

                return await CreateTunnelInternalAsync(connectionInfo, remoteDbHost, remoteDbPort, localPort);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to create SSH tunnel with key auth");
                Console.WriteLine($"[SshTunnelService] SSH tunnel creation with key failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Internal method to create SSH tunnel
        /// </summary>
        private async Task<bool> CreateTunnelInternalAsync(ConnectionInfo connectionInfo, string remoteDbHost, int remoteDbPort, uint localPort)
        {
            try
            {
                // Dispose existing connections
                DisposeConnections();

                // Create SSH client
                _sshClient = new SshClient(connectionInfo);

                // Connect SSH client
                await Task.Run(() => _sshClient.Connect());
                
                if (!_sshClient.IsConnected)
                {
                    _logger.Error("SSH connection failed");
                    Console.WriteLine("[SshTunnelService] SSH connection failed");
                    return false;
                }

                Console.WriteLine("[SshTunnelService] SSH connection established successfully");

                // Create port forwarding (local port 0 means auto-assign)
                _forwardedPort = new ForwardedPortLocal(IPAddress.Loopback.ToString(), localPort, remoteDbHost, (uint)remoteDbPort);
                _sshClient.AddForwardedPort(_forwardedPort);

                // Start port forwarding
                _forwardedPort.Start();

                if (!_forwardedPort.IsStarted)
                {
                    _logger.Error("Port forwarding failed to start");
                    Console.WriteLine("[SshTunnelService] Port forwarding failed to start");
                    return false;
                }

                _logger.Information("SSH tunnel created successfully: Local port {LocalPort} -> {RemoteHost}:{RemotePort}", 
                    _forwardedPort.BoundPort, remoteDbHost, remoteDbPort);
                Console.WriteLine($"[SshTunnelService] ✅ SSH tunnel active: localhost:{_forwardedPort.BoundPort} -> {remoteDbHost}:{remoteDbPort}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "SSH tunnel internal creation failed");
                Console.WriteLine($"[SshTunnelService] SSH tunnel internal creation failed: {ex.Message}");
                DisposeConnections();
                return false;
            }
        }

        /// <summary>
        /// Test SSH connection without creating tunnel
        /// </summary>
        public async Task<bool> TestSshConnectionAsync(string sshHost, int sshPort, string sshUsername, string sshPassword)
        {
            try
            {
                _logger.Information("Testing SSH connection to {SshHost}:{SshPort}", sshHost, sshPort);
                Console.WriteLine($"[SshTunnelService] Testing SSH connection: {sshUsername}@{sshHost}:{sshPort}");

                var connectionInfo = new ConnectionInfo(sshHost, sshPort, sshUsername, new PasswordAuthenticationMethod(sshUsername, sshPassword))
                {
                    Timeout = TimeSpan.FromSeconds(15)
                };

                using var testClient = new SshClient(connectionInfo);
                await Task.Run(() => testClient.Connect());
                
                var isConnected = testClient.IsConnected;
                if (isConnected)
                {
                    Console.WriteLine("[SshTunnelService] ✅ SSH connection test successful");
                    testClient.Disconnect();
                }
                else
                {
                    Console.WriteLine("[SshTunnelService] ❌ SSH connection test failed");
                }

                return isConnected;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "SSH connection test failed");
                Console.WriteLine($"[SshTunnelService] SSH connection test failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get connection string for database through SSH tunnel
        /// </summary>
        public string GetTunnelConnectionString(string database, string username, string password, string additionalParams = "")
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("SSH tunnel is not active");
            }

            var baseParams = "Connect Timeout=120;Command Timeout=120;CharSet=utf8mb4;Connection Lifetime=300;Pooling=true;";
            var connectionString = $"Server=127.0.0.1;Port={LocalPort};Database={database};Uid={username};Pwd={password};{baseParams}{additionalParams}";
            
            _logger.Information("Generated tunnel connection string for database {Database} on local port {LocalPort}", database, LocalPort);
            Console.WriteLine($"[SshTunnelService] Connection string: Server=127.0.0.1;Port={LocalPort};Database={database};...");
            
            return connectionString;
        }

        /// <summary>
        /// Close SSH tunnel and connections
        /// </summary>
        public void CloseTunnel()
        {
            try
            {
                DisposeConnections();
                _logger.Information("SSH tunnel closed successfully");
                Console.WriteLine("[SshTunnelService] SSH tunnel closed");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error closing SSH tunnel");
                Console.WriteLine($"[SshTunnelService] Error closing SSH tunnel: {ex.Message}");
            }
        }

        /// <summary>
        /// Dispose connections
        /// </summary>
        private void DisposeConnections()
        {
            try
            {
                if (_forwardedPort?.IsStarted == true)
                {
                    _forwardedPort.Stop();
                }
                _forwardedPort?.Dispose();
                _forwardedPort = null;

                if (_sshClient?.IsConnected == true)
                {
                    _sshClient.Disconnect();
                }
                _sshClient?.Dispose();
                _sshClient = null;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error disposing SSH connections");
            }
        }

        /// <summary>
        /// Dispose pattern implementation
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                DisposeConnections();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~SshTunnelService()
        {
            Dispose();
        }
    }
} 