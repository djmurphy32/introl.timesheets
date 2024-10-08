﻿using ClosedXML.Excel;

namespace Introl.Tools.Timesheets.ActivityCode.Models;

public class ActCodeParsedSourceModel
{
    public required string ProjectCode { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
    public required IEnumerable<string> ActivityCodes { get; init; }
    public required IDictionary<string, ActCodeEmployee> Employees { get; init; }
    public required IXLWorksheet InputWorksheet { get; init; }
}
