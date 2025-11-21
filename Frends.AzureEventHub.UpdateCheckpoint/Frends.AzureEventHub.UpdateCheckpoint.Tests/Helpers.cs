using System;

namespace Frends.AzureEventHub.UpdateCheckpoint.Tests;

public static class Helpers
{
    public static string ExtractStorageAccountName(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return null;

        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            if (part.StartsWith("AccountName=", StringComparison.OrdinalIgnoreCase))
            {
                return part.Substring("AccountName=".Length);
            }
        }

        return null;
    }

    public static string ExtractEntityPathFromConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return null;

        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            if (part.StartsWith("EntityPath=", StringComparison.OrdinalIgnoreCase))
            {
                return part.Substring("EntityPath=".Length);
            }
        }

        return null;
    }
}
