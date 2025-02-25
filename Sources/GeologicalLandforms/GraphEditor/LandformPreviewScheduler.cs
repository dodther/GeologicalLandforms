using System;
using LunarFramework.GUI;
using TerrainGraph;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.GraphEditor;

public class LandformPreviewScheduler : AsyncPreviewScheduler
{
    public static readonly LandformPreviewScheduler Instance = new();

    private readonly Texture2D _loadingIndicator;

    private LandformPreviewScheduler()
    {
        _loadingIndicator = ContentFinder<Texture2D>.Get("LoadingIndicatorStaticGL");
    }

    protected override void RunOnMainThread(Action action)
    {
        GeologicalLandformsAPI.LunarAPI.LifecycleHooks.DoOnce(action);
    }

    protected override void OnError(PreviewTask task, Exception exception)
    {
        GeologicalLandformsAPI.Logger.Warn("Error occured while generating preview for node: " + task.Node.name, exception);
    }

    private const float IndSize = 50f;

    public override void DrawLoadingIndicator(NodeBase node, Rect rect)
    {
        if (node.OngoingPreviewTask is { Exception: not null })
        {
            LunarGUI.DrawQuad(rect, new Color(0f, 0f, 0f, 0.65f));
            var prevColor = GUI.color;
            GUI.color = new Color(1, 0, 0, 1);
            GUI.DrawTexture(rect.ScaledBy(0.5f), TexButton.CloseXSmall);
            GUI.color = prevColor;
        }
        else if (node.OngoingPreviewTask is not { TimeSinceCreated: < 0.1f, WasIdleBefore: true })
        {
            LunarGUI.DrawQuad(rect, new Color(0f, 0f, 0f, 0.65f));
            var alpha = 1f - (1 + Mathf.Sin(Time.time * 3f)) * 0.4f;
            var cRect = new Rect(rect.center.x - IndSize / 2f, rect.center.y - IndSize / 2f, IndSize, IndSize);
            var prevColor = GUI.color;
            GUI.color = new Color(1, 1, 1, alpha);
            GUI.DrawTexture(cRect, _loadingIndicator);
            GUI.color = prevColor;
        }
    }
}
