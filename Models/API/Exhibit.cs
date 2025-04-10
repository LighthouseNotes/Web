﻿// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Web.Models.API;

public class Exhibit
{
    public required string Id { get; init; }
    public required string Reference { get; init; }
    public required string Description { get; init; }
    public required DateTime DateTimeSeizedProduced { get; init; }
    public required string WhereSeizedProduced { get; init; }
    public required string SeizedBy { get; init; }
    public override string ToString() => Reference;
}

public class AddExhibit
{
    public required string Reference { get; set; }
    public required string Description { get; set; }
    public required DateTime DateTimeSeizedProduced { get; set; }
    public required string WhereSeizedProduced { get; set; }
    public required string SeizedBy { get; set; }
}