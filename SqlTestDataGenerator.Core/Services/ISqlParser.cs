namespace SqlTestDataGenerator.Core.Services
{
    public interface ISqlParser
    {
        SqlDataRequirements ParseQuery(string sqlQuery);
    }
} 