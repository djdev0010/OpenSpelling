using SpellingBee.Application.Ports.Storage;

namespace SpellingBee.App.Composition;

/// <summary>Where this device keeps settings, generated speech and the heard-answer log.</summary>
public sealed record StoragePorts(
    ISettingsStorage Settings,
    ISpeechClipStore SpeechClips,
    IHeardAnswerLog HeardAnswers);
