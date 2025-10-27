
using System.Device.Gpio;
using System.Diagnostics;
using Lights.Web.Services;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;

namespace Lights.Web.AddHostedService;

public class LightService(
    FileService fileService,
    ILogger<LightService> logger) : IHostedService
{
    public GpioController Controller { get; } = new GpioController();
    public List<int> Pins { get; } = [
        26, 19, 13, 6, 5, 11, 9, 10
    ];

    private CancellationTokenSource Token { get; } = new CancellationTokenSource();
    protected Playback? Playback { get; }
    public bool Play { get; set; }
    public string SongFile { get; set; }
    protected ILogger<LightService> Logger { get; } = logger;
    protected FileService FileService { get; } = fileService;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var task = RunLights();
        if (task.IsCompleted)
        {
            return task;
        }

        return Task.CompletedTask;
    }

    public async Task RunLights()
    {
        await Startup();
        try
        {
            while (true)
            {
                if (!string.IsNullOrEmpty(SongFile))
                {
                    await PlayShow();
                }

                await Task.Delay(5000);
            }
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Uncaught error");
        }
    }

    public async Task PlayShow()
    {
        Logger.LogTrace("Starting midi");
        var midiFile = MidiFile.Read(
            FileService.GetPath(SongFile),
            new ReadingSettings
            {
                NoHeaderChunkPolicy = NoHeaderChunkPolicy.Abort,
            });

        var tempMap = midiFile.GetTempoMap();
        var notes = midiFile.GetNotes()
            .Select(n => new { n.Time, Start = true, Note = n })
            .Union(midiFile.GetNotes().Select(n => new { Time = n.EndTime, Start = false, Note = n }))
            .OrderBy(n => n.Time)
            .ToList();
        Logger.LogTrace("Loaded {NoteCount} notes", notes.Count);
        var stopwatch = new Stopwatch();
        while (!Token.Token.IsCancellationRequested && Play)
        {
            stopwatch.Restart();
            foreach (var note in notes)
            {
                if (Token.Token.IsCancellationRequested || !Play)
                {
                    break;
                }

                var timeToPlay = TimeConverter.ConvertTo<MetricTimeSpan>(note.Time, tempMap);
                var sleepTime = timeToPlay.TotalMilliseconds - stopwatch.ElapsedMilliseconds;
                if (sleepTime > 0)
                {
                    await Task.Delay((int)sleepTime);
                }

                Logger.LogTrace("Playing note on channel {ChannelNumber} value: {Low}", note.Note.Channel, note.Start);
                Controller.Write(Pins[note.Note.Channel], note.Start ? PinValue.Low : PinValue.High);
            }

            // We're going to await a tiny bit between plays allowing for the CPU to reclaim some time if it's hammering notes for interrupts
            await Task.Delay(500);
        }

        Clear();
    }

    public async Task Startup()
    {
        Pins.ForEach(p => Controller.OpenPin(p, PinMode.Output));
        foreach (var pin in Pins)
        {

            await Task.Delay(200);
            Controller.Write(pin, PinValue.Low);
        }

        await Task.Delay(500);
        Pins.ForEach(p => Controller.Write(p, PinValue.High));
        await Task.Delay(500);
        Pins.ForEach(p => Controller.Write(p, PinValue.Low));
        await Task.Delay(500);
        Pins.ForEach(p => Controller.Write(p, PinValue.High));
    }

    protected void Clear()
    {
        Pins.ForEach(p => Controller.Write(p, PinValue.High));
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Playback?.Stop();
        Token.Cancel();
        await Task.Delay(1000);
        Playback?.Dispose();
        Clear();
        Pins.ForEach(p => Controller.ClosePin(p));
    }
}
