using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Income;
using CleanArchitecture.Shared.Models.Expenses;
using CleanArchitecture.Shared.Models.Reports;

namespace CleanArchitecture.Application.Services;

public class ReportService(IUnitOfWork unitOfWork) : IReportService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<PaginatedList<IncomeRecordViewModel>> GetIncomeReport(
        Guid? buildingId,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var records = await _unitOfWork.IncomeRecordRepository.GetAllAsync();
        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync();
        var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync();
        var users = await _unitOfWork.UserRepository.GetAllAsync();
        var userMap = users.ToDictionary(u => u.Id, u => u.Name);

        var buildingMap = buildings.ToDictionary(b => b.Id, b => b.BuildingName);
        var apartmentMap = apartments.ToDictionary(a => a.Id, a => a.FlatNumber);

        var query = records.AsQueryable();

        if (buildingId.HasValue)
        {
            query = query.Where(x => x.BuildingId == buildingId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(x => x.PaymentDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(x => x.PaymentDate <= endDate.Value);
        }

        var totalItems = query.Count();
        var pageEntities = query
            .OrderByDescending(x => x.PaymentDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = pageEntities.Select(x => new IncomeRecordViewModel
        {
            Id = x.Id,
            IncomeEntity = x.IncomeEntity,
            IncomeType = x.IncomeType,
            Amount = x.Amount,
            BuildingId = x.BuildingId,
            BuildingName = x.BuildingId.HasValue && buildingMap.TryGetValue(x.BuildingId.Value, out var bName) ? bName : null,
            ApartmentId = x.ApartmentId,
            FlatNumber = x.ApartmentId.HasValue && apartmentMap.TryGetValue(x.ApartmentId.Value, out var fNum) ? fNum : null,
            PaymentDate = x.PaymentDate,
            PaymentMethod = x.PaymentMethod,
            TransactionReference = x.TransactionReference,
            Status = x.Status,
            Notes = x.Notes,
            AttachmentUrl = x.AttachmentUrl,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            CreatedBy = x.CreatedBy.HasValue && userMap.TryGetValue(x.CreatedBy.Value, out var cb) ? cb : null,
            UpdatedBy = x.UpdatedBy.HasValue && userMap.TryGetValue(x.UpdatedBy.Value, out var ub) ? ub : null
        }).ToList();

        return new PaginatedList<IncomeRecordViewModel>(items, totalItems, page, pageSize);
    }

    public async Task<PaginatedList<ExpenseRecordViewModel>> GetExpenseReport(
        Guid? buildingId,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var records = await _unitOfWork.ExpenseRecordRepository.GetAllAsync();
        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync();
        var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync();
        var vendors = await _unitOfWork.VendorRepository.GetAllAsync();
        var users = await _unitOfWork.UserRepository.GetAllAsync();
        var userMap = users.ToDictionary(u => u.Id, u => u.Name);

        var buildingMap = buildings.ToDictionary(b => b.Id, b => b.BuildingName);
        var apartmentMap = apartments.ToDictionary(a => a.Id, a => a.FlatNumber);
        var vendorMap = vendors.ToDictionary(v => v.Id, v => (v.Name, v.CompanyName));

        var query = records.AsQueryable();

        if (buildingId.HasValue)
        {
            query = query.Where(x => x.BuildingId == buildingId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(x => x.ExpenseDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(x => x.ExpenseDate <= endDate.Value);
        }

        var totalItems = query.Count();
        var pageEntities = query
            .OrderByDescending(x => x.ExpenseDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = pageEntities.Select(x => new ExpenseRecordViewModel
        {
            Id = x.Id,
            Category = x.Category,
            ExpenseHead = x.ExpenseHead,
            SpecificItem = x.SpecificItem,
            VendorId = x.VendorId,
            VendorName = x.VendorId.HasValue && vendorMap.TryGetValue(x.VendorId.Value, out var v) ? v.Name : null,
            VendorCompanyName = x.VendorId.HasValue && vendorMap.TryGetValue(x.VendorId.Value, out var v2) ? v2.CompanyName : null,
            Nature = x.Nature,
            Amount = x.Amount,
            Entity = x.Entity,
            BuildingId = x.BuildingId,
            BuildingName = x.BuildingId.HasValue && buildingMap.TryGetValue(x.BuildingId.Value, out var bName) ? bName : null,
            ApartmentId = x.ApartmentId,
            FlatNumber = x.ApartmentId.HasValue && apartmentMap.TryGetValue(x.ApartmentId.Value, out var fNum) ? fNum : null,
            ExpenseDate = x.ExpenseDate,
            PaymentMethod = x.PaymentMethod,
            Status = x.Status,
            Reference = x.Reference,
            AttachmentUrl = x.AttachmentUrl,
            Description = x.Description,
            TankerNumber = x.TankerNumber,
            TimeOfDelivery = x.TimeOfDelivery,
            DeliveryDriverName = x.DeliveryDriverName,
            ManagerInAttendance = x.ManagerInAttendance,
            LitersFilled = x.LitersFilled,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            CreatedBy = x.CreatedBy.HasValue && userMap.TryGetValue(x.CreatedBy.Value, out var cb) ? cb : null,
            UpdatedBy = x.UpdatedBy.HasValue && userMap.TryGetValue(x.UpdatedBy.Value, out var ub) ? ub : null
        }).ToList();

        return new PaginatedList<ExpenseRecordViewModel>(items, totalItems, page, pageSize);
    }

    public async Task<ReportStatsViewModel> GetReportStats(
        Guid? buildingId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var incomes = await _unitOfWork.IncomeRecordRepository.GetAllAsync();
        var expenses = await _unitOfWork.ExpenseRecordRepository.GetAllAsync();

        var incomeQuery = incomes.AsQueryable();
        var expenseQuery = expenses.AsQueryable();

        if (buildingId.HasValue)
        {
            incomeQuery = incomeQuery.Where(x => x.BuildingId == buildingId.Value);
            expenseQuery = expenseQuery.Where(x => x.BuildingId == buildingId.Value);
        }

        if (startDate.HasValue)
        {
            incomeQuery = incomeQuery.Where(x => x.PaymentDate >= startDate.Value);
            expenseQuery = expenseQuery.Where(x => x.ExpenseDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            incomeQuery = incomeQuery.Where(x => x.PaymentDate <= endDate.Value);
            expenseQuery = expenseQuery.Where(x => x.ExpenseDate <= endDate.Value);
        }

        var totalIncomes = incomeQuery.Where(x => x.Status == IncomeStatus.Paid).Sum(x => x.Amount);
        var totalExpenses = expenseQuery.Where(x => x.Status == ExpenseStatus.Paid).Sum(x => x.Amount);

        return new ReportStatsViewModel
        {
            TotalIncomes = totalIncomes,
            TotalExpenses = totalExpenses,
            Net = totalIncomes - totalExpenses
        };
    }

    public async Task<byte[]> ExportReportToCsv(
        string type,
        Guid? buildingId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var csv = new StringBuilder();
        var typeLower = type?.Trim().ToLower() ?? "combined";

        if (typeLower == "income")
        {
            var records = await _unitOfWork.IncomeRecordRepository.GetAllAsync();
            var buildings = await _unitOfWork.BuildingRepository.GetAllAsync();
            var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync();

            var buildingMap = buildings.ToDictionary(b => b.Id, b => b.BuildingName);
            var apartmentMap = apartments.ToDictionary(a => a.Id, a => a.FlatNumber);

            var query = records.AsQueryable();
            if (buildingId.HasValue) query = query.Where(x => x.BuildingId == buildingId.Value);
            if (startDate.HasValue) query = query.Where(x => x.PaymentDate >= startDate.Value);
            if (endDate.HasValue) query = query.Where(x => x.PaymentDate <= endDate.Value);

            var list = query.OrderByDescending(x => x.PaymentDate).ToList();

            csv.AppendLine("ID,IncomeEntity,IncomeType,Amount,BuildingName,FlatNumber,PaymentDate,PaymentMethod,TransactionReference,Status,Notes,CreatedAt");
            foreach (var r in list)
            {
                var bName = r.BuildingId.HasValue && buildingMap.TryGetValue(r.BuildingId.Value, out var bn) ? bn : "";
                var fNum = r.ApartmentId.HasValue && apartmentMap.TryGetValue(r.ApartmentId.Value, out var fn) ? fn : "";

                csv.AppendLine($"\"{r.Id}\",\"{r.IncomeEntity}\",\"{r.IncomeType}\",{r.Amount:F2},\"{EscapeCsv(bName)}\",\"{EscapeCsv(fNum)}\",\"{r.PaymentDate:yyyy-MM-dd}\",\"{r.PaymentMethod}\",\"{EscapeCsv(r.TransactionReference)}\",\"{r.Status}\",\"{EscapeCsv(r.Notes)}\",\"{r.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
            }
        }
        else if (typeLower == "expense")
        {
            var records = await _unitOfWork.ExpenseRecordRepository.GetAllAsync();
            var buildings = await _unitOfWork.BuildingRepository.GetAllAsync();
            var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync();
            var vendors = await _unitOfWork.VendorRepository.GetAllAsync();

            var buildingMap = buildings.ToDictionary(b => b.Id, b => b.BuildingName);
            var apartmentMap = apartments.ToDictionary(a => a.Id, a => a.FlatNumber);
            var vendorMap = vendors.ToDictionary(v => v.Id, v => (v.Name, v.CompanyName));

            var query = records.AsQueryable();
            if (buildingId.HasValue) query = query.Where(x => x.BuildingId == buildingId.Value);
            if (startDate.HasValue) query = query.Where(x => x.ExpenseDate >= startDate.Value);
            if (endDate.HasValue) query = query.Where(x => x.ExpenseDate <= endDate.Value);

            var list = query.OrderByDescending(x => x.ExpenseDate).ToList();

            csv.AppendLine("ID,Category,ExpenseHead,SpecificItem,VendorName,VendorCompanyName,Nature,Amount,Entity,BuildingName,FlatNumber,ExpenseDate,PaymentMethod,Status,Reference,Description,TankerNumber,TimeOfDelivery,DeliveryDriverName,ManagerInAttendance,LitersFilled,CreatedAt");
            foreach (var r in list)
            {
                var vName = r.VendorId.HasValue && vendorMap.TryGetValue(r.VendorId.Value, out var v) ? v.Name : "";
                var vComp = r.VendorId.HasValue && vendorMap.TryGetValue(r.VendorId.Value, out var v2) ? v2.CompanyName : "";
                var bName = r.BuildingId.HasValue && buildingMap.TryGetValue(r.BuildingId.Value, out var bn) ? bn : "";
                var fNum = r.ApartmentId.HasValue && apartmentMap.TryGetValue(r.ApartmentId.Value, out var fn) ? fn : "";
                var deliveryTime = r.TimeOfDelivery.HasValue ? r.TimeOfDelivery.Value.ToString("yyyy-MM-dd HH:mm:ss") : "";

                csv.AppendLine($"\"{r.Id}\",\"{r.Category}\",\"{EscapeCsv(r.ExpenseHead)}\",\"{EscapeCsv(r.SpecificItem)}\",\"{EscapeCsv(vName)}\",\"{EscapeCsv(vComp)}\",\"{r.Nature}\",{r.Amount:F2},\"{r.Entity}\",\"{EscapeCsv(bName)}\",\"{EscapeCsv(fNum)}\",\"{r.ExpenseDate:yyyy-MM-dd}\",\"{r.PaymentMethod}\",\"{r.Status}\",\"{EscapeCsv(r.Reference)}\",\"{EscapeCsv(r.Description)}\",\"{EscapeCsv(r.TankerNumber)}\",\"{deliveryTime}\",\"{EscapeCsv(r.DeliveryDriverName)}\",\"{EscapeCsv(r.ManagerInAttendance)}\",\"{r.LitersFilled}\",\"{r.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
            }
        }
        else
        {
            // Combined Transaction Ledger
            var incomes = await _unitOfWork.IncomeRecordRepository.GetAllAsync();
            var expenses = await _unitOfWork.ExpenseRecordRepository.GetAllAsync();
            var buildings = await _unitOfWork.BuildingRepository.GetAllAsync();
            var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync();

            var buildingMap = buildings.ToDictionary(b => b.Id, b => b.BuildingName);
            var apartmentMap = apartments.ToDictionary(a => a.Id, a => a.FlatNumber);

            var incomeQuery = incomes.AsQueryable();
            var expenseQuery = expenses.AsQueryable();

            if (buildingId.HasValue)
            {
                incomeQuery = incomeQuery.Where(x => x.BuildingId == buildingId.Value);
                expenseQuery = expenseQuery.Where(x => x.BuildingId == buildingId.Value);
            }

            if (startDate.HasValue)
            {
                incomeQuery = incomeQuery.Where(x => x.PaymentDate >= startDate.Value);
                expenseQuery = expenseQuery.Where(x => x.ExpenseDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                incomeQuery = incomeQuery.Where(x => x.PaymentDate <= endDate.Value);
                expenseQuery = expenseQuery.Where(x => x.ExpenseDate <= endDate.Value);
            }

            // Create a unified list of ledger items
            var ledgerItems = new List<LedgerItem>();

            foreach (var inc in incomeQuery.ToList())
            {
                ledgerItems.Add(new LedgerItem
                {
                    Date = inc.PaymentDate,
                    Type = "Income",
                    Category = inc.IncomeType.ToString(),
                    Description = $"Received from {inc.IncomeEntity}",
                    Amount = inc.Amount,
                    BuildingId = inc.BuildingId,
                    ApartmentId = inc.ApartmentId,
                    Status = inc.Status.ToString()
                });
            }

            foreach (var exp in expenseQuery.ToList())
            {
                ledgerItems.Add(new LedgerItem
                {
                    Date = exp.ExpenseDate,
                    Type = "Expense",
                    Category = exp.Category.ToString(),
                    Description = string.IsNullOrEmpty(exp.SpecificItem) ? exp.ExpenseHead ?? "Expense" : exp.SpecificItem,
                    Amount = -exp.Amount, // negative for expense
                    BuildingId = exp.BuildingId,
                    ApartmentId = exp.ApartmentId,
                    Status = exp.Status.ToString()
                });
            }

            var list = ledgerItems.OrderByDescending(x => x.Date).ToList();

            csv.AppendLine("Date,Type,Category,Description,Amount,Status,BuildingName,FlatNumber");
            foreach (var r in list)
            {
                var bName = r.BuildingId.HasValue && buildingMap.TryGetValue(r.BuildingId.Value, out var bn) ? bn : "";
                var fNum = r.ApartmentId.HasValue && apartmentMap.TryGetValue(r.ApartmentId.Value, out var fn) ? fn : "";

                csv.AppendLine($"\"{r.Date:yyyy-MM-dd}\",\"{r.Type}\",\"{r.Category}\",\"{EscapeCsv(r.Description)}\",{r.Amount:F2},\"{r.Status}\",\"{EscapeCsv(bName)}\",\"{EscapeCsv(fNum)}\"");
            }
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    private static string EscapeCsv(string? val)
    {
        if (string.IsNullOrEmpty(val)) return "";
        return val.Replace("\"", "\"\"");
    }

    private class LedgerItem
    {
        public DateTime Date { get; set; }
        public string Type { get; set; } = null!;
        public string Category { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Status { get; set; } = null!;
        public Guid? BuildingId { get; set; }
        public Guid? ApartmentId { get; set; }
    }
}
