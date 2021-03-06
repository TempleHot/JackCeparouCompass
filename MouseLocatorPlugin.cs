using Turbo.Plugins.Default;
using Turbo.Plugins.Jack.Decorators;

namespace Turbo.Plugins.Jack
{
    public class MouseLocatorPlugin : BasePlugin, IInGameTopPainter
    {
        public TopCircleDecorator MouseDecorator { get; set; }

        public MouseLocatorPlugin()
        {
            Enabled = true;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);
            MouseDecorator = new TopCircleDecorator(hud)
            {
                Brush = Hud.Render.CreateBrush(178, 255, 255, 255, 5),
                HasShadow = true,
                Radius = 31,
                //RadiusTransformator = new StandardPingRadiusTransformator(Hud, 250) { RadiusMinimumMultiplier = 1f, RadiusMaximumMultiplier = 1.4f },
            };
        }

        public void PaintTopInGame(ClipState clipState)
        {
            if (Hud.Render.UiHidden) return;
            //if (Hud.InTown) return;
            if (clipState != ClipState.BeforeClip) return;

            MouseDecorator.Paint(Hud.Window.CursorX, Hud.Window.CursorY);
        }
    }
}