using System.Globalization;
using System.Text.Json;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

namespace ArizaTakipMobil.Services;

// Telefonun donanım özellikleri: titreşim, kamera, bildirim, mikrofon, parmak izi, paylaşım, adres bulma
public class CihazServisi
{
    private static readonly HttpClient Http = HttpOlustur();

    private static HttpClient HttpOlustur()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("ArizaTakipDemo/1.0 (portfolyo projesi)");
        return http;
    }

    public bool AndroidMi => DeviceInfo.Current.Platform == DevicePlatform.Android;

    // ---------- Titreşim ----------

    public void Tik()
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
    }

    public void UzunTitre()
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); } catch { }
    }

    public void AcilTitresim()
    {
        try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(700)); } catch { }
    }

    // ---------- Paylaşım ----------

    public async Task Paylas(string baslik, string metin)
    {
        try
        {
            await Share.Default.RequestAsync(new ShareTextRequest { Title = baslik, Text = metin });
        }
        catch { }
    }

    // ---------- Fotoğraf ----------

    // Kamerayla çeker ya da galeriden seçer, küçültüp uygulama klasörüne kaydeder, dosya yolunu döner
    public async Task<string?> FotoAl(bool kamera)
    {
        try
        {
            FileResult? secilen;
            if (kamera)
            {
                if (!MediaPicker.Default.IsCaptureSupported) return null;
                var izin = await Permissions.RequestAsync<Permissions.Camera>();
                if (izin != PermissionStatus.Granted) return null;
                secilen = await MediaPicker.Default.CapturePhotoAsync();
            }
            else
            {
#pragma warning disable CS0618
                secilen = await MediaPicker.Default.PickPhotoAsync();
#pragma warning restore CS0618
            }

            if (secilen == null) return null;

            var hedef = System.IO.Path.Combine(FileSystem.AppDataDirectory, "foto_" + Guid.NewGuid().ToString("N")[..12] + ".jpg");
            await using var kaynak = await secilen.OpenReadAsync();
            await KucultVeKaydet(kaynak, hedef);
            return hedef;
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> FotoVerisi(string? yol)
    {
        try
        {
            if (string.IsNullOrEmpty(yol) || !System.IO.File.Exists(yol)) return null;
            var baytlar = await System.IO.File.ReadAllBytesAsync(yol);
            return "data:image/jpeg;base64," + Convert.ToBase64String(baytlar);
        }
        catch
        {
            return null;
        }
    }

    public void FotoSil(string? yol)
    {
        try
        {
            if (!string.IsNullOrEmpty(yol) && System.IO.File.Exists(yol)) System.IO.File.Delete(yol);
        }
        catch { }
    }

    private static async Task KucultVeKaydet(Stream kaynak, string hedef)
    {
        using var bellek = new MemoryStream();
        await kaynak.CopyToAsync(bellek);
        var baytlar = bellek.ToArray();

#if ANDROID
        try
        {
            // Telefon fotoğrafı yan çekildiyse düzelt
            int aci = 0;
            try
            {
                using var exifAkis = new MemoryStream(baytlar);
                var exif = new global::Android.Media.ExifInterface(exifAkis);
                aci = exif.GetAttributeInt(global::Android.Media.ExifInterface.TagOrientation, 1) switch
                {
                    6 => 90,
                    3 => 180,
                    8 => 270,
                    _ => 0
                };
            }
            catch { }

            using var orijinal = await global::Android.Graphics.BitmapFactory.DecodeByteArrayAsync(baytlar, 0, baytlar.Length);
            if (orijinal != null)
            {
                // En uzun kenar en fazla 1024 piksel
                float oran = Math.Min(1f, 1024f / Math.Max(orijinal.Width, orijinal.Height));
                var matris = new global::Android.Graphics.Matrix();
                matris.PostScale(oran, oran);
                if (aci != 0) matris.PostRotate(aci);

                using var yeni = global::Android.Graphics.Bitmap.CreateBitmap(orijinal, 0, 0, orijinal.Width, orijinal.Height, matris, true);
                await using var cikis = System.IO.File.Create(hedef);
                await yeni.CompressAsync(global::Android.Graphics.Bitmap.CompressFormat.Jpeg!, 80, cikis);
                return;
            }
        }
        catch { }
#endif
        await System.IO.File.WriteAllBytesAsync(hedef, baytlar);
    }

    // ---------- Telefon bildirimi ----------

    public async Task BildirimIzniIste()
    {
#if ANDROID
        try
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
                await Permissions.RequestAsync<Permissions.PostNotifications>();
        }
        catch { }
#else
        await Task.CompletedTask;
#endif
    }

    public void BildirimGoster(string baslik, string metin)
    {
#if ANDROID
        try
        {
            var context = global::Android.App.Application.Context;
            const string kanalId = "ariza_durum";

            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                var yonetici = (global::Android.App.NotificationManager?)context.GetSystemService(global::Android.Content.Context.NotificationService);
                var kanal = new global::Android.App.NotificationChannel(kanalId, "Arıza durumu", global::Android.App.NotificationImportance.High)
                {
                    Description = "Arıza durum değişiklikleri"
                };
                yonetici?.CreateNotificationChannel(kanal);
            }

            // Bildirime dokununca uygulama açılsın
            global::Android.App.PendingIntent? acilacak = null;
            var intent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName!);
            if (intent != null)
            {
                acilacak = global::Android.App.PendingIntent.GetActivity(context, 0, intent,
                    global::Android.App.PendingIntentFlags.Immutable | global::Android.App.PendingIntentFlags.UpdateCurrent);
            }

            var olusturucu = new global::AndroidX.Core.App.NotificationCompat.Builder(context, kanalId)
                .SetContentTitle(baslik)
                .SetContentText(metin)
                .SetSmallIcon(context.ApplicationInfo!.Icon)
                .SetAutoCancel(true)
                .SetPriority(global::AndroidX.Core.App.NotificationCompat.PriorityHigh);

            if (acilacak != null) olusturucu.SetContentIntent(acilacak);

            global::AndroidX.Core.App.NotificationManagerCompat.From(context).Notify(Random.Shared.Next(1000, 99999), olusturucu.Build());
        }
        catch { }
#endif
    }

    // ---------- Sesle yazma (Android konuşma tanıma) ----------

    public async Task<string?> SesliDinle()
    {
#if ANDROID
        try
        {
            var izin = await Permissions.RequestAsync<Permissions.Microphone>();
            if (izin != PermissionStatus.Granted) return null;

            var aktivite = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (aktivite == null || !global::Android.Speech.SpeechRecognizer.IsRecognitionAvailable(aktivite)) return null;

            var tanima = global::Android.Speech.SpeechRecognizer.CreateSpeechRecognizer(aktivite);
            if (tanima == null) return null;

            var dinleyici = new SesDinleyici();
            tanima.SetRecognitionListener(dinleyici);

            var istek = new global::Android.Content.Intent(global::Android.Speech.RecognizerIntent.ActionRecognizeSpeech);
            istek.PutExtra(global::Android.Speech.RecognizerIntent.ExtraLanguageModel, global::Android.Speech.RecognizerIntent.LanguageModelFreeForm);
            istek.PutExtra(global::Android.Speech.RecognizerIntent.ExtraLanguage, "tr-TR");
            istek.PutExtra(global::Android.Speech.RecognizerIntent.ExtraMaxResults, 1);
            tanima.StartListening(istek);

            var biten = await Task.WhenAny(dinleyici.Sonuc.Task, Task.Delay(TimeSpan.FromSeconds(15)));
            string? metin = biten == dinleyici.Sonuc.Task ? dinleyici.Sonuc.Task.Result : null;

            try { tanima.StopListening(); tanima.Destroy(); } catch { }
            return metin;
        }
        catch
        {
            return null;
        }
#else
        await Task.CompletedTask;
        return null;
#endif
    }

    // ---------- Parmak izi ----------

    public bool ParmakIziDestekleniyor
    {
        get
        {
#if ANDROID
            try
            {
                if (!OperatingSystem.IsAndroidVersionAtLeast(28)) return false;
                var context = global::Android.App.Application.Context;
                var kilit = (global::Android.App.KeyguardManager?)context.GetSystemService(global::Android.Content.Context.KeyguardService);
                var parmakIziVar = context.PackageManager?.HasSystemFeature(global::Android.Content.PM.PackageManager.FeatureFingerprint) == true;
                return kilit?.IsDeviceSecure == true && parmakIziVar;
            }
            catch
            {
                return false;
            }
#else
            return false;
#endif
        }
    }

    public async Task<bool> ParmakIziDogrula()
    {
#if ANDROID
        try
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(28)) return false;
            var aktivite = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (aktivite == null || aktivite.MainExecutor == null) return false;

            var sonuc = new TaskCompletionSource<bool>();
            var pencere = new global::Android.Hardware.Biometrics.BiometricPrompt.Builder(aktivite)
                .SetTitle("Personel girişi")
                .SetSubtitle("Devam etmek için parmak izini okut")
                .SetNegativeButton("Vazgeç", aktivite.MainExecutor, new VazgecDinleyici(sonuc))
                .Build();

            pencere.Authenticate(new global::Android.OS.CancellationSignal(), aktivite.MainExecutor, new ParmakIziGeriBildirim(sonuc));

            var biten = await Task.WhenAny(sonuc.Task, Task.Delay(TimeSpan.FromSeconds(30)));
            return biten == sonuc.Task && sonuc.Task.Result;
        }
        catch
        {
            return false;
        }
#else
        await Task.CompletedTask;
        return false;
#endif
    }

    // ---------- Konumdan adres bulma (OpenStreetMap Nominatim) ----------

    public async Task<string?> AdresBul(double enlem, double boylam)
    {
        try
        {
            var url = string.Create(CultureInfo.InvariantCulture,
                $"https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat={enlem}&lon={boylam}&zoom=18&addressdetails=1&accept-language=tr");

            using var yanit = await Http.GetAsync(url);
            if (!yanit.IsSuccessStatusCode) return null;

            await using var akis = await yanit.Content.ReadAsStreamAsync();
            using var belge = await JsonDocument.ParseAsync(akis);
            var kok = belge.RootElement;

            if (kok.TryGetProperty("address", out var adres))
            {
                string? Al(params string[] anahtarlar)
                {
                    foreach (var anahtar in anahtarlar)
                    {
                        if (adres.TryGetProperty(anahtar, out var deger) && deger.ValueKind == JsonValueKind.String)
                            return deger.GetString();
                    }
                    return null;
                }

                var parcalar = new List<string>();
                var mahalle = Al("neighbourhood", "suburb", "quarter", "village");
                if (mahalle != null) parcalar.Add(mahalle.Contains("Mah") ? mahalle : mahalle + " Mah.");

                var yol = Al("road", "pedestrian", "street");
                var no = Al("house_number");
                if (yol != null) parcalar.Add(no != null ? $"{yol} No:{no}" : yol);

                var ilce = Al("town", "city_district", "district", "county");
                var il = Al("province", "city", "state");
                if (ilce != null) parcalar.Add(ilce);
                if (il != null && il != ilce) parcalar.Add(il);

                if (parcalar.Count > 0) return string.Join(", ", parcalar);
            }

            return kok.TryGetProperty("display_name", out var tamAd) ? tamAd.GetString() : null;
        }
        catch
        {
            return null;
        }
    }
}

#if ANDROID
// Konuşma tanıma sonucunu dinler
internal class SesDinleyici : global::Java.Lang.Object, global::Android.Speech.IRecognitionListener
{
    public TaskCompletionSource<string?> Sonuc { get; } = new();

    public void OnResults(global::Android.OS.Bundle? results)
    {
        var liste = results?.GetStringArrayList(global::Android.Speech.SpeechRecognizer.ResultsRecognition);
        Sonuc.TrySetResult(liste != null && liste.Count > 0 ? liste[0] : null);
    }

    public void OnError(global::Android.Speech.SpeechRecognizerError error) => Sonuc.TrySetResult(null);
    public void OnBeginningOfSpeech() { }
    public void OnBufferReceived(byte[]? buffer) { }
    public void OnEndOfSpeech() { }
    public void OnEvent(int eventType, global::Android.OS.Bundle? @params) { }
    public void OnPartialResults(global::Android.OS.Bundle? partialResults) { }
    public void OnReadyForSpeech(global::Android.OS.Bundle? @params) { }
    public void OnRmsChanged(float rmsdB) { }
}

// Parmak izi başarılı olunca
internal class ParmakIziGeriBildirim : global::Android.Hardware.Biometrics.BiometricPrompt.AuthenticationCallback
{
    private readonly TaskCompletionSource<bool> _sonuc;
    public ParmakIziGeriBildirim(TaskCompletionSource<bool> sonuc) => _sonuc = sonuc;

    public override void OnAuthenticationSucceeded(global::Android.Hardware.Biometrics.BiometricPrompt.AuthenticationResult? result)
        => _sonuc.TrySetResult(true);
}

// Parmak izi penceresinde "Vazgeç"
internal class VazgecDinleyici : global::Java.Lang.Object, global::Android.Content.IDialogInterfaceOnClickListener
{
    private readonly TaskCompletionSource<bool> _sonuc;
    public VazgecDinleyici(TaskCompletionSource<bool> sonuc) => _sonuc = sonuc;

    public void OnClick(global::Android.Content.IDialogInterface? dialog, int which) => _sonuc.TrySetResult(false);
}
#endif
