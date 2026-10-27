using NAudio.Wave;

namespace BatNotif.App.Audio;

/// <summary>Rejoue une source en boucle sans fin, sans coupure audible (FR-015).</summary>
internal sealed class LoopingSampleProvider(ISampleProvider source, Action rewind) : ISampleProvider
{
    public WaveFormat WaveFormat => source.WaveFormat;

    public int Read(Span<float> buffer)
    {
        var total = 0;
        var emptyReads = 0;
        while (total < buffer.Length)
        {
            var read = source.Read(buffer[total..]);
            if (read == 0)
            {
                // Source vide ou illisible : on évite une boucle infinie.
                if (++emptyReads > 1)
                {
                    break;
                }

                rewind();
                continue;
            }

            emptyReads = 0;
            total += read;
        }

        return total;
    }
}

/// <summary>Source mémoire mono (sons intégrés) rejouable depuis le début.</summary>
internal sealed class MemorySampleProvider(float[] samples, int sampleRate) : ISampleProvider
{
    private int _position;

    public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);

    public int Read(Span<float> buffer)
    {
        var available = Math.Min(buffer.Length, samples.Length - _position);
        samples.AsSpan(_position, available).CopyTo(buffer);
        _position += available;
        return available;
    }

    public void Rewind() => _position = 0;
}
