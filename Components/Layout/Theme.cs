namespace Web.Components.Layout;

// ReSharper disable once ClassNeverInstantiated.Global
public class Theme
{
    // Custom Mudblazor theme for Lighthouse Notes
    public static readonly MudTheme LighthouseNotesTheme = new()
    {
        // Light mode
        PaletteLight = new PaletteLight
        {
            Primary = "#1CBFFF",
            Secondary = "#10698D",

            AppbarBackground = "#03131A"
        },

        // Dark mode
        PaletteDark = new PaletteDark
        {
            Primary = "#1CBFFF",
            Secondary = "#10698D",

            AppbarBackground = "#03131A",
            DrawerBackground = "#03131A"
        },

        // Z index
        ZIndex = new ZIndex
        {
            Drawer = 1301,
            Snackbar = 10010
        },

        // Typography
        Typography = new Typography
        {
            // All text
            Default = new DefaultTypography
            {
                FontFamily = ["Sora", "sans-serif"]
            },

            // Heading 1
            H1 = new H1Typography
            {
                FontSize = "4rem"
            },

            // Heading 2
            H2 = new H2Typography
            {
                FontSize = "3rem"
            }
        }
    };
}