namespace BatNotif.App.Audio;

/// <summary>
/// Sons par défaut générés par le code, sans fichier externe (research R6) :
/// alerte basse = bips graves descendants, alerte haute = bips aigus montants.
/// Chaque motif se termine par un silence pour être joué en boucle.
/// </summary>
public static class BuiltInSounds
{
    public const int SampleRate = 44100;

    private static readonly Lazy<float[]> LowPattern = new(() => Render([880, 660, 440], 0.20, 0.08, 0.9));
    private static readonly Lazy<float[]> HighPattern = new(() => Render([523, 659, 784, 1047], 0.15, 0.06, 0.9));

    /// <summary>Échantillons mono 44,1 kHz du motif demandé.</summary>
    public static float[] For(AlertSound sound) => sound == AlertSound.Low ? LowPattern.Value : HighPattern.Value;

    private static float[] Render(double[] frequencies, double toneSeconds, double gapSeconds, double tailSeconds)
    {
        var tone = (int)(toneSeconds * SampleRate);
        var gap = (int)(gapSeconds * SampleRate);
        var tail = (int)(tailSeconds * SampleRate);
        var samples = new float[frequencies.Length * (tone + gap) + tail];
        var fade = (int)(0.01 * SampleRate); // 10 ms d'attaque / relâchement : pas de clic audible.

        var offset = 0;
        foreach (var frequency in frequencies)
        {
            for (var i = 0; i < tone; i++)
            {
                var envelope = Math.Min(1.0, Math.Min(i, tone - 1 - i) / (double)fade);
                // Fondamentale + harmonique douce : plus présent qu'un sinus pur sur les petits haut-parleurs.
                var t = i / (double)SampleRate;
                var value = 0.75 * Math.Sin(2 * Math.PI * frequency * t) + 0.25 * Math.Sin(4 * Math.PI * frequency * t);
                samples[offset + i] = (float)(0.6 * envelope * value);
            }

            offset += tone + gap;
        }

        return samples;
    }
}
