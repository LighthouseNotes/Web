﻿using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using Web.Models;

namespace Web.Components.Pages.Case;

public class ExhibitsBase : ComponentBase
{
    [Parameter] public required string CaseId { get; set; }
    [Inject] private LighthouseNotesAPIGet LighthouseNotesAPIGet { get; set; } = default!;
    [Inject] private LighthouseNotesAPIPost LighthouseNotesAPIPost { get; set; } = default!;
    [Inject] private ISettingsService SettingsService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    // Page variables
    protected PageLoad? PageLoad;
    protected API.Case SCase = null!;
    protected AddExhibit Model = new();
    protected Settings Settings = new();
    protected MudDataGrid<API.Exhibit> ExhibitsTable = null!;

    // Class variables
    private readonly ReadOnlyCollection<TimeZoneInfo> _timeZones = TimeZoneInfo.GetSystemTimeZones();

    // Add exhibit form
    protected class AddExhibit
    {
        [Required] public string Reference { get; set; } = null!;

        [Required] public string Description { get; set; } = null!;

        [Required] public DateTime? DateSeizedProduced { get; set; }

        [Required] public TimeSpan? TimeSeizedProduced { get; set; }

        [Required] public TimeZoneInfo TimeZone { get; set; } = null!;

        [Required] public string WhereSeizedProduced { get; set; } = null!;

        [Required] public string SeizedBy { get; set; } = null!;
    }

    // On parameters set 
    protected override async Task OnParametersSetAsync()
    {
        // Get case, exhibits and users from API
        SCase = await LighthouseNotesAPIGet.Case(CaseId);

        // Mark page load as complete 
        PageLoad?.LoadComplete();
    }

    // After page render
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // If settings is null the get the settings
        if (Settings.Auth0UserId == null || Settings.OrganizationId == null || Settings.UserId == null ||
            Settings.S3Endpoint == null)
        {
            // Get the settings redirect url
            string? settingsRedirect = await SettingsService.CheckOrSet();
            
            // If the settings redirect url is not null then redirect 
            if (settingsRedirect != null)
            {
                NavigationManager.NavigateTo(settingsRedirect, true);
            }
            
            // Use the setting service to retrieve the settings
            Settings = await SettingsService.Get();

            Model.TimeZone = TimeZoneInfo.FindSystemTimeZoneById(Settings.TimeZone);

            // Re-render component
            await InvokeAsync(StateHasChanged);
        }
    }

    protected async Task<GridData<API.Exhibit>> LoadGridData(GridState<API.Exhibit> state)
    {
        // Create sort string
        string sortString = "";

        // If sort definition is set then set sort string
        if (state.SortDefinitions.Count == 1)
            // if descending is true then column-name desc else column-name asc
            sortString = state.SortDefinitions.First().Descending
                ? $"{state.SortDefinitions.First().SortBy} desc"
                : $"{state.SortDefinitions.First().SortBy} asc";

        // Fetch cases from API
        (API.Pagination, List<API.Exhibit>?) exhibits =
            await LighthouseNotesAPIGet.Exhibits(CaseId, state.Page + 1, state.PageSize, sortString);

        // Create grid data
        GridData<API.Exhibit> data = new()
        {
            Items = exhibits.Item2!,
            TotalItems = exhibits.Item1.Total
        };

        // Return grid data
        return data;
    }

    // On valid form submission
    protected async Task OnValidSubmit(EditContext context)
    {
        // Get user timezone 
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(Model.TimeZone.Id);

        // Parse date time from form
        DateTime localDateTimeSeizedProduced = DateTime.Parse(
            $"{DateOnly.FromDateTime(Model.DateSeizedProduced!.Value)} {Model.TimeSeizedProduced!.Value.ToString()}");

        // Convert date time from local date time to UTC
        DateTime dateTimeSeizedProduced =
            TimeZoneInfo.ConvertTimeBySystemTimeZoneId(localDateTimeSeizedProduced, timeZone.Id, TimeZoneInfo.Utc.Id);

        // Create exhibit
        API.AddExhibit addExhibit = new()
        {
            Reference = Model.Reference,
            Description = Model.Description,
            DateTimeSeizedProduced = dateTimeSeizedProduced,
            WhereSeizedProduced = Model.WhereSeizedProduced,
            SeizedBy = Model.SeizedBy
        };

        // Call api to create exhibit and get result
        await LighthouseNotesAPIPost.Exhibit(CaseId, addExhibit);

        // Reload the exhibits table
        await ExhibitsTable.ReloadServerData();

        // Clear the form fields
        Model = new AddExhibit();

        // Re-render component
        await InvokeAsync(StateHasChanged);

        Snackbar.Add("Successfully created the exhibit!", Severity.Success);
    }

    // Time zone search 
    protected Task<IEnumerable<TimeZoneInfo>> TimeZoneSearch(string value)
    {
        // If text is null or empty, show complete list else return match for timezone 
        return Task.FromResult(string.IsNullOrEmpty(value)
            ? _timeZones
            : _timeZones.Where(x => x.DisplayName.Contains(value, StringComparison.InvariantCultureIgnoreCase)));
    }
}