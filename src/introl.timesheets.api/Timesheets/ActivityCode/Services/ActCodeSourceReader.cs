﻿using ClosedXML.Excel;
using Introl.Timesheets.Api.Extensions;
using Introl.Timesheets.Api.Timesheets.ActivityCode.Constants;
using Introl.Timesheets.Api.Timesheets.ActivityCode.Models;

namespace Introl.Timesheets.Api.Timesheets.ActivityCode.Services;

public class ActCodeSourceReader : IActCodeSourceReader
{
    public ActCodeTimesheetModel Process(XLWorkbook workbook)
    {
        var summaryWorksheet = workbook.Worksheets.Worksheet("Summary");

        var (startDate, endDate) = GetStartAndEndDate(summaryWorksheet);
        var keyPositions = GetActivityCodeKeyPositions(summaryWorksheet);
        var employees = GetEmployees(summaryWorksheet, keyPositions);

        return new ActCodeTimesheetModel
        {
            StartDate = startDate,
            EndDate = endDate,
            InputWorksheet = summaryWorksheet,
            Employees = employees
        };
    }

    private IDictionary<string, ActCodeEmployee> GetEmployees(IXLWorksheet worksheet,
        ActCodeSourceKeyPositions keyPositions)
    {
        var startRow = keyPositions.TitleRow + 1;
        var endRow = keyPositions.TotalHoursRow - 1;
        var result = new Dictionary<string, ActCodeEmployee>();

        for (var i = startRow; i < endRow; i++)
        {
            var memberCode = worksheet.Cell(i, keyPositions.MemberCodeCol);
            if (string.IsNullOrWhiteSpace(memberCode.GetString()))
            {
                continue;
            }

            var totalHours = ConvertToRoundedHours(worksheet.Cell(i, keyPositions.TrackedHoursCol).GetString());
            var date = worksheet.Cell(i, keyPositions.DateCol).GetDateTime();
            var startTime = worksheet.Cell(i, keyPositions.StartTimeCol).GetDateTime();

            var startDateTime = new DateTime(date.Year, date.Month, date.Day, startTime.Hour, startTime.Minute, 0);

            var hours = new ActCodeHours
            {
                StartTime = startDateTime,
                ActivityCode = worksheet.Cell(i, keyPositions.ActivityCodeCol).GetString(),
                Hours = totalHours
            };
            if (result.ContainsKey(memberCode.GetString()))
            {
                result[memberCode.GetString()].ActivityCodeHours.Add(hours);
            }
            else
            {
                result[memberCode.GetString()] = new ActCodeEmployee
                {
                    Name = worksheet.Cell(i, keyPositions.NameCol).GetString(),
                    MemberCode = memberCode.GetString(),
                    ActivityCodeHours = new List<ActCodeHours> { hours }
                };
            }
        }

        return result;
    }

    private ActCodeSourceKeyPositions GetActivityCodeKeyPositions(IXLWorksheet worksheet)
    {
        return new ActCodeSourceKeyPositions
        {
            TitleRow = worksheet.FindSingleCellByValue(ActCodeSourceConstants.ActivityCode).Address.RowNumber,
            ActivityCodeCol = worksheet.FindSingleCellByValue(ActCodeSourceConstants.ActivityCode).Address.ColumnNumber,
            DateCol = worksheet.FindSingleCellByValue(ActCodeSourceConstants.Date, true).Address.ColumnNumber,
            NameCol = worksheet.FindSingleCellByValue(ActCodeSourceConstants.Name).Address.ColumnNumber,
            MemberCodeCol = worksheet.FindSingleCellByValue(ActCodeSourceConstants.MemberCode).Address.ColumnNumber,
            StartTimeCol = worksheet.FindSingleCellByValue(ActCodeSourceConstants.StartTime).Address.ColumnNumber,
            TrackedHoursCol =
                worksheet.FindSingleCellByValue(ActCodeSourceConstants.TrackedHours).Address.ColumnNumber,
            BillableRateCol =
                worksheet.FindSingleCellByValue(ActCodeSourceConstants.BillableRate).Address.ColumnNumber,
            TotalHoursRow = worksheet.FindSingleCellByValue(ActCodeSourceConstants.TotalHours).Address.RowNumber,
        };
    }

    private double ConvertToRoundedHours(string inputHours)
    {
        if (!inputHours.Contains(":"))
        {
            if (double.TryParse(inputHours, out var parsedHours))
            {
                return parsedHours;
            }

            return 0;
        }

        var splitHours = inputHours.Split(':');
        var hours = double.Parse(splitHours[0]);
        var minutes = double.Parse(splitHours[1]);

        return minutes switch
        {
            >= 0 and <= 14 => hours,
            >= 15 and <= 44 => hours + 0.5,
            _ => hours + 1
        };
    }

    private (DateOnly startDate, DateOnly endDate) GetStartAndEndDate(IXLWorksheet worksheet)
    {
        var dateRangeCell = worksheet.FindSingleCellByValue(ActCodeSourceConstants.DateRange);
        var dateString = dateRangeCell.CellRight().GetString();
        var splitDates = dateString.Split(" - ");

        var startDate = DateOnly.Parse(splitDates[0]);
        var endDate = DateOnly.Parse(splitDates[1]);

        return (startDate, endDate);
    }
}

public interface IActCodeSourceReader
{
    ActCodeTimesheetModel Process(XLWorkbook workbook);
}