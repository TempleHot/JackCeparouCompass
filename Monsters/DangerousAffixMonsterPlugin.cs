﻿namespace Turbo.Plugins.JackCeparouCompass.Monsters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Turbo.Plugins.Default;

    public delegate string AffixNameFunc(ISnoMonsterAffix affix);

    public class DangerousAffixMonsterDefinition
    {
        public MonsterAffix Affix { get; private set; }
        public int Priority { get; set; }

        public string AffixName { get; set; }
        public AffixNameFunc AffixNameFunc { get; set; }

        public WorldDecoratorCollection EliteDecorators { get; set; }
        public IFont EliteLabelFont { get; set; }

        public WorldDecoratorCollection MinionDecorators { get; set; }
        public IFont MinionLabelFont { get; set; }
        public bool ShowMinionDecorators { get; set; }
        public bool ShowMinionAffixesNames { get; set; }

        public DangerousAffixMonsterDefinition(MonsterAffix affix)
        {
            Affix = affix;
            AffixNameFunc = (a) => string.Empty;
        }
    }

    public class DangerousAffixMonsterPlugin : BasePlugin
    {
        public Dictionary<MonsterAffix, DangerousAffixMonsterDefinition> Affixes { get; set; }

        public IShapePainter DefaultMapShapePainter { get; set; }
        public StandardPingRadiusTransformator DefaultRadiusTransformator { get; set; }
        public IBrush DefaultBackgroundBrush { get; set; }
        public IBrush DefaultForegroundBrush { get; set; }
        public IFont DefaultEliteAffixesFont { get; set; }
        public IFont DefaultMinionAffixesFont { get; set; }

        private const float _bgEliteRadius = 8;
        private const float _fgEliteRadius = 4;
        private const float _bgMinionRadius = 6;
        private const float _fgMinionRadius = 2;

        public DangerousAffixMonsterPlugin()
        {
            Enabled = true;
            Order = 20100;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);

            Affixes = new Dictionary<MonsterAffix, DangerousAffixMonsterDefinition>();

            DefaultMapShapePainter = new CircleShapePainter(Hud);
            DefaultRadiusTransformator = new StandardPingRadiusTransformator(Hud, 500);
            DefaultBackgroundBrush = Hud.Render.CreateBrush(255, 0, 0, 0, 0);
            DefaultForegroundBrush = Hud.Render.CreateBrush(255, 255, 0, 0, 0);
            DefaultEliteAffixesFont = Hud.Render.CreateFont("tahoma", 10f, 200, 255, 255, 0, false, false, 128, 0, 0, 0, true);
            DefaultMinionAffixesFont = Hud.Render.CreateFont("tahoma", 7f, 200, 255, 255, 0, false, false, 128, 0, 0, 0, true);

            //foreach (MonsterAffix affix in Enum.GetValues(typeof(MonsterAffix))) { DefineDangerousAffix(affix, (a) => a.NameLocalized.Substring(0, 2)); }
        }

        public override void PaintWorld(WorldLayer layer)
        {
            var monsters = Hud.Game.AliveMonsters.Where(m => m.Rarity == ActorRarity.Champion || m.Rarity == ActorRarity.Rare || m.Rarity == ActorRarity.RareMinion);
            foreach (var monster in monsters)
            {
                var dangerousAffixes = monster.AffixSnoList
                    .Join(Affixes,
                        snoAffix => snoAffix.Affix,
                        dangerous => dangerous.Key,
                        (snoAffix, dangerous) => dangerous.Value)
                    .ToList();
                    //.Where(a => Affixes.ContainsKey(a.Affix))
                    //.Select(a => Affixes[a.Affix]).ToList();

                if (dangerousAffixes.Count == 0) continue;

                dangerousAffixes.Sort((a, b) => -a.Priority.CompareTo(b.Priority));

                switch (monster.Rarity)
                {
                    case ActorRarity.Champion:
                    case ActorRarity.Rare:
                        dangerousAffixes.First().EliteDecorators.Paint(layer, monster, monster.FloorCoordinate, null);

                        if (dangerousAffixes.Count == 1)
                            DrawAffixName(monster, dangerousAffixes.First());
                        else if (dangerousAffixes.Any())
                            DrawAffixNames(monster, dangerousAffixes, GetCombinedLabelsText(monster, dangerousAffixes));
                        break;

                    case ActorRarity.RareMinion:
                        var minionAffix = dangerousAffixes.FirstOrDefault(a => a.ShowMinionDecorators);

                        if (minionAffix != null)
                            minionAffix.MinionDecorators.Paint(layer, monster, monster.FloorCoordinate, null);

                        dangerousAffixes = dangerousAffixes.Where(a => a.ShowMinionAffixesNames).ToList();

                        if (dangerousAffixes.Count == 1)
                            DrawAffixName(monster, dangerousAffixes.First());
                        else if (dangerousAffixes.Any())
                            DrawAffixNames(monster, dangerousAffixes.Where(a => a.ShowMinionAffixesNames).ToList(), GetCombinedLabelsText(monster, dangerousAffixes));
                        break;
                }
            }
        }

        // TODO : refactor these monstruous signatures.. ><
        public DangerousAffixMonsterDefinition DefineDangerousAffix(MonsterAffix affix, string affixName, int priority = 0,
            IFont eliteFont = null, IFont minionFont = null, bool showMinionDecorators = false, bool showMinionAffixesNames = false,
            float bgEliteRadius = _bgEliteRadius, float fgEliteRadius = _fgEliteRadius, float bgMinionRadius = _bgMinionRadius, float fgMinionRadius = _fgMinionRadius,
            IBrush bgBrush = null, IShapePainter bgShapePainter = null, bool bgPing = false, IRadiusTransformator bgRadiusTransformator = null,
            IBrush fgBrush = null, IShapePainter fgShapePainter = null, bool fgPing = false, IRadiusTransformator fgRadiusTransformator = null)
        {
            AffixNameFunc affixNameFunc;
            if (affixName == null)
                affixNameFunc = (a) => string.Empty;
            else
                affixNameFunc = (a) => affixName;

            return DefineDangerousAffix(affix, affixNameFunc, priority,
                eliteFont, minionFont, showMinionDecorators, showMinionAffixesNames,
                bgEliteRadius, fgEliteRadius, bgMinionRadius, fgMinionRadius,
                bgBrush, bgShapePainter, bgPing, bgRadiusTransformator,
                fgBrush, fgShapePainter, fgPing, fgRadiusTransformator);
        }

        public DangerousAffixMonsterDefinition DefineDangerousAffix(MonsterAffix affix, AffixNameFunc affixNameFunc, int priority = 0,
            IFont eliteFont = null, IFont minionFont = null, bool showMinionDecorators = false, bool showMinionAffixesNames = false,
            float bgEliteRadius = _bgEliteRadius, float fgEliteRadius = _fgEliteRadius, float bgMinionRadius = _bgMinionRadius, float fgMinionRadius = _fgMinionRadius,
            IBrush bgBrush = null, IShapePainter bgShapePainter = null, bool bgPing = false, IRadiusTransformator bgRadiusTransformator = null,
            IBrush fgBrush = null, IShapePainter fgShapePainter = null, bool fgPing = false, IRadiusTransformator fgRadiusTransformator = null)
        {
            bgBrush = bgBrush ?? DefaultBackgroundBrush;
            fgBrush = fgBrush ?? DefaultForegroundBrush;
            bgShapePainter = bgShapePainter ?? DefaultMapShapePainter;
            fgShapePainter = fgShapePainter ?? DefaultMapShapePainter;
            eliteFont = eliteFont ?? DefaultEliteAffixesFont;
            minionFont = minionFont ?? DefaultMinionAffixesFont;

            if (bgPing)
                bgRadiusTransformator = bgRadiusTransformator ?? DefaultRadiusTransformator;
            else
                bgRadiusTransformator = null;

            if (fgPing)
                fgRadiusTransformator = fgRadiusTransformator ?? DefaultRadiusTransformator;
            else
                fgRadiusTransformator = null;

            var affixDef = new DangerousAffixMonsterDefinition(affix)
            {
                EliteDecorators = CreateDecorators(bgEliteRadius, fgEliteRadius, bgBrush, fgBrush, bgShapePainter, fgShapePainter, bgRadiusTransformator, fgRadiusTransformator),
                MinionDecorators = CreateDecorators(bgMinionRadius, fgMinionRadius, bgBrush, fgBrush, bgShapePainter, fgShapePainter, bgRadiusTransformator, fgRadiusTransformator),
                EliteLabelFont = eliteFont,
                MinionLabelFont = minionFont,
                ShowMinionDecorators = showMinionDecorators,
                ShowMinionAffixesNames = showMinionAffixesNames,
                AffixNameFunc = affixNameFunc,
                Priority = priority,

            };

            affixDef.MinionDecorators.ToggleDecorators<MapShapeDecorator>(showMinionDecorators);
            affixDef.MinionDecorators.ToggleDecorators<MapLabelDecorator>(showMinionAffixesNames);

            Affixes[affix] = affixDef;
            return affixDef;
        }

        private WorldDecoratorCollection CreateDecorators(float bgRadius, float fgRadius, IBrush bgBrush, IBrush fgBrush,
            IShapePainter bgShapePainter, IShapePainter fgShapePainter, IRadiusTransformator bgRadiusTransformator, IRadiusTransformator fgRadiusTransformator)
        {
            return new WorldDecoratorCollection(
                new MapShapeDecorator(Hud)
                {
                    Brush = bgBrush,
                    Radius = bgRadius,
                    ShapePainter = bgShapePainter,
                    RadiusTransformator = bgRadiusTransformator,
                },
                new MapShapeDecorator(Hud)
                {
                    Brush = fgBrush,
                    Radius = fgRadius,
                    ShapePainter = fgShapePainter,
                    RadiusTransformator = fgRadiusTransformator,
                }
            );
        }

        public void DrawAffixName(IMonster monster, DangerousAffixMonsterDefinition affix)
        {
            var font = monster.Rarity == ActorRarity.RareMinion ? affix.MinionLabelFont : affix.EliteLabelFont;
            var text = affix.AffixName ?? (affix.AffixName = affix.AffixNameFunc.Invoke(monster.AffixSnoList.First(snoa => snoa.Affix == affix.Affix)));
            var layout = font.GetTextLayout(text);
            float mapX, mapY;
            Hud.Render.GetMinimapCoordinates(monster.FloorCoordinate.X, monster.FloorCoordinate.Y, out mapX, out mapY);

            font.DrawText(layout, mapX - layout.Metrics.Width / 2, mapY);
        }

        public void DrawAffixNames(IMonster monster, IList<DangerousAffixMonsterDefinition> affixes, string combinedNames)
        {
            // get an estimation of combined labels size
            var first = affixes.FirstOrDefault();
            if (first == null) return;

            var firstFont = (monster.Rarity == ActorRarity.RareMinion ? first.MinionLabelFont : first.EliteLabelFont);
            var firstLayout = firstFont.GetTextLayout(combinedNames);
            var spacerLayout = firstFont.GetTextLayout(":");
            var offsetX = -firstLayout.Metrics.Width / 2;

            affixes.ForEach(a =>
            {
                var font = monster.Rarity == ActorRarity.RareMinion ? a.MinionLabelFont : a.EliteLabelFont;
                var text = a.AffixName ?? (a.AffixName = a.AffixNameFunc.Invoke(monster.AffixSnoList.First(snoa => snoa.Affix == a.Affix)));
                var layout = font.GetTextLayout(text);
                float mapX, mapY;
                Hud.Render.GetMinimapCoordinates(monster.FloorCoordinate.X, monster.FloorCoordinate.Y, out mapX, out mapY);

                font.DrawText(layout, mapX + offsetX, mapY);
                offsetX += layout.Metrics.Width + spacerLayout.Metrics.Width;
            });
        }

        private static string GetCombinedLabelsText(IMonster monster, IEnumerable<DangerousAffixMonsterDefinition> affixes)
        {
            return string.Join(":",
                affixes.Select(
                    a =>
                        a.AffixName ??
                        (a.AffixName = a.AffixNameFunc.Invoke(monster.AffixSnoList.First(snoa => snoa.Affix == a.Affix)))));
        }
    }
}

/*
private IEnumerable<string> GetAffixesNames(IEnumerable<ISnoMonsterAffix> affixes, bool minion)
{
    foreach (var affix in affixes)
    {
        if (minion)
        {
            if (!ShowMinionAffixesNames.ContainsKey(affix.Affix)) continue;
            if (!ShowMinionAffixesNames[affix.Affix]) continue;
        }

        if (!DangerousEliteDecorators.ContainsKey(affix.Affix)) continue;
        if (!DangerousAffixNames.ContainsKey(affix.Affix)) continue;
        if (DangerousAffixNames[affix.Affix] == null) continue;

        var text = DangerousAffixNames[affix.Affix].Invoke(affix);
        if (string.IsNullOrWhiteSpace(text)) continue;

        yield return text;
    }
}

private IEnumerable<TextLayout> GetAffixesLayouts(IEnumerable<ISnoMonsterAffix> affixes, bool minion)
{
    foreach (var affix in affixes)
    {
        if (minion)
        {
            if (!ShowMinionAffixesNames.ContainsKey(affix.Affix)) continue;
            if (!ShowMinionAffixesNames[affix.Affix]) continue;
            if (!DangerousMinionLabelFonts.ContainsKey(affix.Affix)) continue;
        }

        if (!DangerousEliteDecorators.ContainsKey(affix.Affix)) continue;
        if (!DangerousAffixNames.ContainsKey(affix.Affix)) continue;
        if (DangerousAffixNames[affix.Affix] == null) continue;
        if (!DangerousEliteLabelFonts.ContainsKey(affix.Affix)) continue;

        var text = DangerousAffixNames[affix.Affix].Invoke(affix);
        if (string.IsNullOrWhiteSpace(text)) continue;

        yield return (minion ? DangerousMinionLabelFonts : DangerousEliteLabelFonts)[affix.Affix].GetTextLayout(text);
    }
}
/**/