using Microsoft.Maui.Storage;

namespace ArizaTakipMobil.Services;

// Karanlık mod ve ses ayarı telefonda kalıcı saklanır
public class AyarServisi
{
    public event Action? Degisti;

    public bool KaranlikMod
    {
        get => Preferences.Default.Get("karanlik_mod", false);
        set
        {
            Preferences.Default.Set("karanlik_mod", value);
            Degisti?.Invoke();
        }
    }

    public bool SesAcik
    {
        get => Preferences.Default.Get("ses_acik", true);
        set
        {
            Preferences.Default.Set("ses_acik", value);
            Degisti?.Invoke();
        }
    }
}
