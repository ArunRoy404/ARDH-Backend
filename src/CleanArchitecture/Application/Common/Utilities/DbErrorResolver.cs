using System.Text.RegularExpressions;
using CleanArchitecture.Shared.Domain.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Common.Utilities;

/// <summary>
/// Resolves database-level exceptions (unique-constraint violations, foreign-key
/// conflicts, null/truncation errors, ...) into human-readable error messages so
/// callers never have to surface raw SQL/EF text to the API consumer.
/// </summary>
public static class DbErrorResolver
{
    /// <summary>
    /// Walks the exception chain and returns a friendly message plus the most
    /// appropriate HTTP error code for the underlying database failure.
    /// </summary>
    public static (string Message, ErrorCode ErrorCode) Resolve(Exception exception)
    {
        var current = exception;
        while (current != null)
        {
            switch (current)
            {
                case SqlException sqlException:
                    return ResolveSqlException(sqlException);
                case DbUpdateException:
                    // Fall through to inner exception to find the actual SqlException.
                    break;
            }

            current = current.InnerException;
        }

        return ("The request could not be completed due to a database error. Please try again.",
            ErrorCode.Internal);
    }

    private static (string Message, ErrorCode ErrorCode) ResolveSqlException(SqlException sqlException)
    {
        switch (sqlException.Number)
        {
            // 2601: Cannot insert duplicate key row (unique index)
            // 2627: Violation of UNIQUE KEY constraint
            case 2601:
            case 2627:
                return (BuildDuplicateMessage(sqlException.Message), ErrorCode.BadRequest);

            // 547: FOREIGN KEY constraint conflict
            case 547:
                return ("This record cannot be saved or deleted because it is linked to other records.",
                    ErrorCode.BadRequest);

            // 515: Cannot insert the value NULL into a column that does not allow nulls
            case 515:
                return ("A required value was not provided. Please complete all required fields before saving.",
                    ErrorCode.BadRequest);

            // 8152: String or binary data would be truncated
            case 8152:
                return ("One of the values you entered is too long. Please shorten it and try again.",
                    ErrorCode.BadRequest);

            default:
                return ("A database error occurred while processing your request. Please try again.",
                    ErrorCode.Internal);
        }
    }

    private static string BuildDuplicateMessage(string sqlMessage)
    {
        // Example SQL Server messages:
        //   Cannot insert duplicate key row in object 'dbo.Users' with unique index 'IX_users_Email'. ...
        //   Violation of UNIQUE KEY constraint 'UQ_Users_Email'. Cannot insert duplicate key in object 'dbo.Users'. ...
        var objectMatch = Regex.Match(sqlMessage, @"object\s+'([^']+)'", RegexOptions.IgnoreCase);
        var indexMatch = Regex.Match(sqlMessage, @"(?:index|constraint)\s+'([^']+)'", RegexOptions.IgnoreCase);

        var tableName = objectMatch.Success
            ? objectMatch.Groups[1].Value.Replace("dbo.", string.Empty, StringComparison.OrdinalIgnoreCase)
            : string.Empty;
        var indexName = indexMatch.Success ? indexMatch.Groups[1].Value : string.Empty;

        var entity = MapEntity(tableName);
        var field = MapField(indexName);

        if (!string.IsNullOrWhiteSpace(field))
        {
            return $"A {entity} with this {field} already exists. Please use a different {field}.";
        }

        return "A record with the same value already exists. Please check your input for duplicates.";
    }

    private static string MapEntity(string tableName)
        => tableName.ToLowerInvariant() switch
        {
            "users" => "user",
            "owners" => "owner",
            "tenants" => "tenant",
            "vendors" => "vendor",
            "buildings" => "building",
            "apartments" => "apartment",
            "equipment" => "equipment",
            "amccontracts" => "AMC contract",
            _ => "record"
        };

    private static string MapField(string indexName)
    {
        if (string.IsNullOrWhiteSpace(indexName))
        {
            return string.Empty;
        }

        var lower = indexName.ToLowerInvariant();

        if (lower.Contains("email"))
        {
            return "email address";
        }

        if (lower.Contains("phonenumber") || lower.Contains("phone"))
        {
            return "phone number";
        }

        if (lower.Contains("idnumber") || lower.Contains("id_number"))
        {
            return "ID number";
        }

        if (lower.Contains("gstnumber") || lower.Contains("gst"))
        {
            return "GST number";
        }

        if (lower.Contains("serialnumber") || lower.Contains("serial"))
        {
            return "serial number";
        }

        if (lower.Contains("buildingname") || lower.Contains("building_name"))
        {
            return "name";
        }

        if (lower.Contains("flatnumber") || lower.Contains("flat"))
        {
            return "flat number";
        }

        if (lower.Contains("nestawayid") || lower.Contains("nestaway"))
        {
            return "Nestaway ID";
        }

        if (lower.Contains("accountnumber") || lower.Contains("account"))
        {
            return "account number";
        }

        if (lower.Contains("amccode") || lower.Contains("amc_code"))
        {
            return "AMC code";
        }

        return string.Empty;
    }
}
