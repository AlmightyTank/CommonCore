using SPTarkov.Server.Core.Models.Spt.Mod;

namespace CommonCore.Items.Services.ItemServiceHelpers;

public static class LocaleConverter
{
    public static Dictionary<string, LocaleDetails>? ToLocaleDetails(
    Dictionary<string, Dictionary<string, string>>? locales)
    {
        if (locales == null)
            return null;

        var result = new Dictionary<string, LocaleDetails>();

        foreach (var (lang, entries) in locales)
        {
            var locale = new LocaleDetails();

            foreach (var (k, v) in entries)
            {
                if (string.IsNullOrWhiteSpace(k))
                    continue;

                // Strip itemId prefix → get last word
                var key = k.Split(' ').Last();

                switch (key)
                {
                    case "Name":
                        locale.Name = v;
                        break;

                    case "ShortName":
                        locale.ShortName = v;
                        break;

                    case "Description":
                        locale.Description = v;
                        break;
                }
            }

            result[lang] = locale;
        }

        return result;
    }
}