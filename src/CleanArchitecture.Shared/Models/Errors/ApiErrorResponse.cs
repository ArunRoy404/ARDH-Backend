using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CleanArchitecture.Shared.Models.Errors;

public class ApiErrorResponse
{
    public bool Success { get; set; } = false;
    public string Message { get; set; } = "An error occurred.";
    public List<ApiErrorDetail> Errors { get; set; } = new();

    public ApiErrorResponse() { }

    public ApiErrorResponse(string message)
    {
        Message = message;
    }

    public ApiErrorResponse(string message, List<ApiErrorDetail> errors)
    {
        Message = message;
        Errors = errors;
    }
}

public class ApiErrorDetail
{
    private string _message = string.Empty;
    private string? _property;
    private string? _code;

    public string? Code
    {
        get => _code;
        set
        {
            _code = value;
            UpdateErrorMessage();
        }
    }

    public string Message
    {
        get => _message;
        set
        {
            _message = value;
            UpdateErrorMessage();
        }
    }

    [JsonIgnore]
    public string? Property
    {
        get => _property;
        set
        {
            _property = value;
            UpdateErrorMessage();
        }
    }

    public string? ErrorMessage { get; set; }

    public ApiErrorDetail() { }

    public ApiErrorDetail(string? code, string message, string? property = null)
    {
        _code = code;
        _property = property;
        _message = message;
        UpdateErrorMessage();
    }

    private void UpdateErrorMessage()
    {
        ErrorMessage = GetFriendlyErrorMessage(_message, _property, _code);
    }

    private static string GetFriendlyErrorMessage(string message, string? property, string? code)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "An error occurred.";

        if (message.Contains("The request field is required.", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("A non-empty request body is required.", StringComparison.OrdinalIgnoreCase))
        {
            return "Please provide the request body.";
        }

        // 1. JSON conversion errors
        if (message.Contains("JSON value could not be converted", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("Could not convert", StringComparison.OrdinalIgnoreCase))
        {
            if (message.Contains("buildingId", StringComparison.OrdinalIgnoreCase))
                return "You need to choose a valid building.";
            if (message.Contains("ownerId", StringComparison.OrdinalIgnoreCase))
                return "You need to choose a valid owner.";
            if (message.Contains("tenantId", StringComparison.OrdinalIgnoreCase))
                return "You need to choose a valid tenant.";
            if (message.Contains("apartmentId", StringComparison.OrdinalIgnoreCase))
                return "You need to choose a valid apartment.";
            if (message.Contains("vendorId", StringComparison.OrdinalIgnoreCase))
                return "You need to choose a valid vendor.";
            if (message.Contains("equipmentId", StringComparison.OrdinalIgnoreCase))
                return "You need to choose a valid equipment.";
            if (message.Contains("flatNumber", StringComparison.OrdinalIgnoreCase))
                return "Please enter a valid flat number.";
            if (message.Contains("floor", StringComparison.OrdinalIgnoreCase))
                return "Please enter a valid floor number.";
            if (message.Contains("areaSqft", StringComparison.OrdinalIgnoreCase))
                return "Please enter a valid area in square feet.";
            if (message.Contains("bedrooms", StringComparison.OrdinalIgnoreCase))
                return "Please enter a valid number of bedrooms.";
            if (message.Contains("bathrooms", StringComparison.OrdinalIgnoreCase))
                return "Please enter a valid number of bathrooms.";
            if (message.Contains("expectedRent", StringComparison.OrdinalIgnoreCase))
                return "Please enter a valid rent amount.";
            if (message.Contains("maintenanceCharge", StringComparison.OrdinalIgnoreCase))
                return "Please enter a valid maintenance charge.";
            if (message.Contains("waterCharge", StringComparison.OrdinalIgnoreCase))
                return "Please enter a valid water charge.";
            if (message.Contains("date", StringComparison.OrdinalIgnoreCase))
                return "Please enter a valid date.";
            return "The value provided is invalid. Please check the format.";
        }

        // 2. Existence / Uniqueness Constraint / Friendly Errors
        if (message.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("already in use", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("is in use", StringComparison.OrdinalIgnoreCase))
        {
            if (message.Contains("Flat number", StringComparison.OrdinalIgnoreCase) || message.Contains("flatNumber", StringComparison.OrdinalIgnoreCase))
                return "The flat number is already in use in this building.";
            if (message.Contains("building", StringComparison.OrdinalIgnoreCase))
                return "A building with this name already exists.";
            if (message.Contains("email", StringComparison.OrdinalIgnoreCase))
                return "This email address is already registered.";
            if (message.Contains("phone", StringComparison.OrdinalIgnoreCase))
                return "This phone number is already registered.";
            if (message.Contains("nestaway", StringComparison.OrdinalIgnoreCase))
                return "This Nestaway ID is already in use.";
            return message;
        }

        if (message.Contains("does not exist", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("not exist", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            if (message.Contains("building", StringComparison.OrdinalIgnoreCase))
                return "The chosen building does not exist.";
            if (message.Contains("owner", StringComparison.OrdinalIgnoreCase))
                return "The chosen owner does not exist.";
            if (message.Contains("tenant", StringComparison.OrdinalIgnoreCase))
                return "The chosen tenant does not exist.";
            if (message.Contains("apartment", StringComparison.OrdinalIgnoreCase))
                return "The chosen apartment does not exist.";
            if (message.Contains("vendor", StringComparison.OrdinalIgnoreCase))
                return "The chosen vendor does not exist.";
            if (message.Contains("equipment", StringComparison.OrdinalIgnoreCase))
                return "The chosen equipment does not exist.";
            return "The requested record does not exist.";
        }

        if (message.Contains("Invalid Admin password", StringComparison.OrdinalIgnoreCase))
        {
            return "The administrator password you entered is incorrect.";
        }

        // 3. Required Fields
        if (message.Contains("is required", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("must not be empty", StringComparison.OrdinalIgnoreCase))
        {
            if (message.Contains("Building ID", StringComparison.OrdinalIgnoreCase) || message.Contains("buildingId", StringComparison.OrdinalIgnoreCase))
                return "Please select a building.";
            if (message.Contains("Owner ID", StringComparison.OrdinalIgnoreCase) || message.Contains("ownerId", StringComparison.OrdinalIgnoreCase))
                return "Please select an owner.";
            if (message.Contains("Apartment ID", StringComparison.OrdinalIgnoreCase) || message.Contains("apartmentId", StringComparison.OrdinalIgnoreCase))
                return "Please select an apartment.";
            if (message.Contains("Flat number", StringComparison.OrdinalIgnoreCase) || message.Contains("flatNumber", StringComparison.OrdinalIgnoreCase))
                return "Please enter a flat number.";
            if (message.Contains("Apartment type", StringComparison.OrdinalIgnoreCase) || message.Contains("apartmentType", StringComparison.OrdinalIgnoreCase))
                return "Please specify the apartment type.";
            if (message.Contains("Parking slot", StringComparison.OrdinalIgnoreCase) || message.Contains("parkingSlot", StringComparison.OrdinalIgnoreCase))
                return "Please enter a parking slot.";
            if (message.Contains("Full name", StringComparison.OrdinalIgnoreCase) || message.Contains("fullName", StringComparison.OrdinalIgnoreCase))
                return "Please enter the full name.";
            if (message.Contains("Phone", StringComparison.OrdinalIgnoreCase) || message.Contains("phone", StringComparison.OrdinalIgnoreCase))
                return "Please enter the phone number.";
            if (message.Contains("Email", StringComparison.OrdinalIgnoreCase) || message.Contains("email", StringComparison.OrdinalIgnoreCase))
                return "Please enter the email address.";
            if (message.Contains("Address", StringComparison.OrdinalIgnoreCase) || message.Contains("address", StringComparison.OrdinalIgnoreCase))
                return "Please enter the address.";
            if (message.Contains("City", StringComparison.OrdinalIgnoreCase) || message.Contains("city", StringComparison.OrdinalIgnoreCase))
                return "Please enter the city.";
            if (message.Contains("State", StringComparison.OrdinalIgnoreCase) || message.Contains("state", StringComparison.OrdinalIgnoreCase))
                return "Please enter the state.";
            if (message.Contains("Country", StringComparison.OrdinalIgnoreCase) || message.Contains("country", StringComparison.OrdinalIgnoreCase))
                return "Please enter the country.";
            return message;
        }

        return message;
    }
}
