using BingoMode.BingoChallenges;
using BingoMode.BingoSteamworks;
using Expedition;
using Menu;
using Menu.Remix.MixedUI;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BingoMode.BingoMenu
{
    public class CustomizerDialog : Dialog
    {
        private enum SubMenu
        {
            Settings,
            Types
        }

        private enum State
        {
            Opening,
            Open,
            Closing,
            Closed
        }

        private static Vector2 ScreenOffset => new(Custom.GetScreenOffsets()[0], 0f);
        private const float TITLE_MARGIN = 36f;
        private const float MARGIN = 5f;
        private const float DIVIDER_WIDTH = 400f;
        private const float DIVIDER_THICKNESS = 2f;

        private FSprite title;
        #region Header
        private const float HEADER_Y = BODY_Y + BODY_HEIGHT;
        private const float BUTTON_SIZE = 40f;
        private const float BUTTON_SPACING = 150f;
        private const float SELECTOR_CHEAT_OFFSET = 3f;

        private MenuLabel description;
        private FSprite[] dividers;
        private MenuLabel selector;
        private SymbolButton typesButton;
        private SymbolButton randomize;
        private SymbolButton settingsButton;
        #endregion
        #region Body
        private const float BODY_Y = FOOTER_Y + CLOSE_HEIGHT + MARGIN;
        private const float BODY_HEIGHT = 224f;

        private const float SETTING_HEIGHT = 30f;
        private const float SETTING_SPACING = 10f;

        private const float TYPES_HEIGHT = 20f;
        private const float TYPES_SPACING = 0f;

        private const float SLIDER_WIDTH = 10f;
        private const float SLIDER_OFFSET = 15f; // not actually modifiable from here
        private const float SLIDER_DEAD_ZONE = 21f; // not actually modifiable from here

        private TypeButton[] types;
        private VerticalSlider slider;
        private List<ChallengeSetting> challengeSettings;
        #endregion
        #region Footer
        private const float FOOTER_Y = 220f;
        private const float CLOSE_HEIGHT = 35f;
        private const float CLOSE_MIN_WIDTH = 85f;

        private SimpleButton closeButton;
        #endregion
        private BingoButton owner;
        private SubMenu subMenu;
        private State state;
        private float currentAlpha;
        private float lastAlpha;
        private float targetAlpha;
        private float sliderF;
        private float scrollVelocity;

        public CustomizerDialog(ProcessManager manager, BingoButton owner) : base(manager)
        {
            this.owner = owner;
            size = Custom.rainWorld.screenSize;
            pages[0].pos.y = size.y;
            pages[0].lastPos.y = size.y;

            title = new FSprite("customizer")
            {
                x = ScreenOffset.x + size.x / 2f,
                y = ScreenOffset.y + size.y - TITLE_MARGIN,
                anchorX = 0.5f,
                anchorY = 1f,
                shader = manager.rainWorld.Shaders["MenuText"]
            };
            pages[0].Container.AddChild(title);

            #region Header
            description = new MenuLabel(
                    this,
                    pages[0],
                    owner.challenge.description.WrapText(false, DIVIDER_WIDTH - 2f * MARGIN),
                    ScreenOffset + new Vector2(size.x / 2f, HEADER_Y + 3f * MARGIN + BUTTON_SIZE),
                    Vector2.zero,
                    false);
            description.label.anchorY = 0f;
            description.label.shader = manager.rainWorld.Shaders["MenuText"];
            pages[0].subObjects.Add(description);

            dividers = new FSprite[3];
            for (int i = 0; i < 3; i++)
            {
                dividers[i] = new FSprite("pixel")
                {
                    scaleY = DIVIDER_THICKNESS,
                    scaleX = DIVIDER_WIDTH,
                    anchorX = 0.5f
                };
                pages[0].Container.AddChild(dividers[i]);
            }

            selector = new MenuLabel(this, pages[0], ">       <", Vector2.zero, Vector2.zero, true);
            pages[0].subObjects.Add(selector);

            typesButton = new SymbolButton(
                    this,
                    pages[0],
                    "custommenu",
                    "CHALLENGE_TYPES",
                    ScreenOffset + new Vector2((size.x - BUTTON_SIZE) / 2f - BUTTON_SPACING, HEADER_Y + MARGIN))
            { size = Vector2.one * BUTTON_SIZE };
            typesButton.roundedRect.size = Vector2.one * BUTTON_SIZE;
            typesButton.symbolSprite.scale = 0.6f;
            pages[0].subObjects.Add(typesButton);

            randomize = new SymbolButton(
                    this,
                    pages[0],
                    "Sandbox_Randomize",
                    "RANDOMIZE_VARIABLE",
                    ScreenOffset + new Vector2((size.x - BUTTON_SIZE) / 2f, HEADER_Y + MARGIN))
            { size = Vector2.one * BUTTON_SIZE };
            randomize.roundedRect.size = Vector2.one * BUTTON_SIZE;
            pages[0].subObjects.Add(randomize);

            settingsButton = new SymbolButton(
                    this,
                    pages[0],
                    "settingscog",
                    "CHALLENGE_SETTINGS",
                    ScreenOffset + new Vector2((size.x - BUTTON_SIZE) / 2f + BUTTON_SPACING, HEADER_Y + MARGIN))
            { size = Vector2.one * BUTTON_SIZE };
            settingsButton.roundedRect.size = Vector2.one * BUTTON_SIZE;
            pages[0].subObjects.Add(settingsButton);
            #endregion
            #region Body
            slider = new VerticalSlider(
                    this,
                    pages[0],
                    "",
                    ScreenOffset + new Vector2((size.x + DIVIDER_WIDTH) / 2f - MARGIN - SLIDER_OFFSET - SLIDER_WIDTH / 2f, BODY_Y + MARGIN),
                    new Vector2(SLIDER_WIDTH, BODY_HEIGHT - 2 * MARGIN - SLIDER_DEAD_ZONE),
                    BingoEnums.CustomizerSlider,
                    true)
            { floatValue = 1f };
            foreach (FSprite line in slider.lineSprites)
                line.alpha = 0f;
            slider.subtleSliderNob.outerCircle.alpha = 0f;
            pages[0].subObjects.Add(slider);

            List<Challenge> challengeTypes = [.. BingoData.GetValidChallengeList(ExpeditionData.slugcatPlayer)];
            types = new TypeButton[challengeTypes.Count];
            for (int i = 0; i < challengeTypes.Count; i++)
            {
                types[i] = new TypeButton(
                        this,
                        pages[0],
                        new Vector2(DIVIDER_WIDTH - 2 * MARGIN, TYPES_HEIGHT),
                        challengeTypes[i])
                { pos = ScreenOffset + new Vector2((size.x - DIVIDER_WIDTH) / 2f + MARGIN, 0f) };
                pages[0].subObjects.Add(types[i]);
            }
            types = [.. types.OrderBy(x => x.text.text)];
            #endregion
            #region Footer
            float closeWidth = Mathf.Max(CLOSE_MIN_WIDTH, LabelTest.GetWidth(Translate("CLOSE"), false) + 10f);
            closeButton = new SimpleButton(
                    this,
                    pages[0],
                    Translate("CLOSE"),
                    "CLOSE",
                    ScreenOffset + new Vector2((size.x - closeWidth) / 2f, FOOTER_Y),
                    new Vector2(closeWidth, CLOSE_HEIGHT));
            pages[0].subObjects.Add(closeButton);
            #endregion

            state = State.Opening;
            targetAlpha = 1f;
            SwitchTo(SubMenu.Settings);
            ResetSettings(owner.challenge as BingoChallenge);
            UpdateChallenge();
        }

        public void ResetSettings(BingoChallenge ch)
        {
            if (challengeSettings != null)
            {
                foreach (ChallengeSetting s in challengeSettings)
                {
                    s.RemoveSprites();
                    pages[0].RemoveSubObject(s);
                }
            }
            
            Vector2 pos = ScreenOffset + new Vector2((size.x - MARGIN - SLIDER_WIDTH) / 2f, HEADER_Y - MARGIN - SETTING_HEIGHT);
            challengeSettings = [];
            foreach (object setting in ch.Settings())
            {
                ChallengeSetting s = new(
                        this,
                        pages[0],
                        pos,
                        new Vector2(DIVIDER_WIDTH - 3 * MARGIN - SLIDER_WIDTH, SETTING_HEIGHT),
                        setting,
                        0.5f)
                { Alpha = subMenu == SubMenu.Settings ? 1f : 0f };
                challengeSettings.Add(s);
                pages[0].subObjects.Add(s);
                pos.y -= SETTING_HEIGHT + SETTING_SPACING;
            }
        }

        public override void SliderSetValue(Slider slider, float f)
        {
            if (slider.ID == BingoEnums.CustomizerSlider)
                sliderF = f;
        }

        public override float ValueOfSlider(Slider slider)
        {
            if (slider.ID == BingoEnums.CustomizerSlider)
                return sliderF;
            return 0f;
        }

        public override void Update()
        {
            base.Update();
            ScrollInput();
        }

        public override void GrafUpdate(float timeStacker)
        {
            const float ALPHA_DARK = 0.95f;
            const float EPSILON = 0.01f;

            base.GrafUpdate(timeStacker);
            Slide(timeStacker);

            Vector2 pagePos = pages[0].DrawPos(timeStacker) + ScreenOffset;
            float uAlpha = currentAlpha;
            if (state == State.Opening || state == State.Closing)
                uAlpha = Mathf.Pow(Mathf.Lerp(lastAlpha, currentAlpha, timeStacker), 1.5f);
            
            if (uAlpha >= 1f - EPSILON)
                uAlpha = 1f;
            darkSprite.alpha = uAlpha * ALPHA_DARK;

            pages[0].pos.y = Mathf.Lerp(size.y, EPSILON, uAlpha);
            title.SetPosition(pagePos + new Vector2(size.x / 2f, size.y - TITLE_MARGIN));
            dividers[0].SetPosition(new Vector2(size.x / 2f, HEADER_Y + 2f * MARGIN + BUTTON_SIZE) + pagePos);
            dividers[1].SetPosition(new Vector2(size.x / 2f, HEADER_Y) + pagePos);
            dividers[2].SetPosition(new Vector2(size.x / 2f, BODY_Y) + pagePos);

            description.label.alpha = darkSprite.alpha;
            foreach (FSprite divider in dividers)
                divider.alpha = darkSprite.alpha;
            foreach (FSprite line in slider.lineSprites)
                line.alpha = uAlpha;
            slider.subtleSliderNob.outerCircle.alpha = uAlpha;

            switch (subMenu)
            {
                case SubMenu.Settings:
                    selector.label.color = settingsButton.MyColor(timeStacker);
                    DrawSettings(timeStacker);
                    break;
                case SubMenu.Types:
                    selector.label.color = typesButton.MyColor(timeStacker);
                    DrawTypes(timeStacker);
                    break;
            }
        }

        public void UpdateChallenge()
        {
            if (owner.challenge is BingoEatChallenge c)
            {
                c.isCreature = Array.IndexOf(ChallengeUtils.FoodTypes, c.foodType.Value) >= Array.IndexOf(ChallengeUtils.FoodTypes, "VultureGrub");
            }
            else if (owner.challenge is BingoDontUseItemChallenge cc)
            {
                var l = ChallengeUtils.GetCorrectListForChallenge("banitem");
                cc.isFood = Array.IndexOf(l, cc.item.Value) < (l.Length - ChallengeUtils.Bannable.Length);
                if (cc.isFood) cc.isCreature = Array.IndexOf(ChallengeUtils.FoodTypes, cc.item.Value) >= Array.IndexOf(ChallengeUtils.FoodTypes, "VultureGrub");
            }
            else if (owner.challenge is BingoVistaChallenge ccc)
            {
                ccc.region = ccc.room.Value.Substring(0, 2);
                
                ccc.location = ChallengeUtils.BingoVistaLocations[ccc.region][ccc.room.Value];
                BingoVistaChallenge.ModifyVistaPositions(ccc);
            }
            owner.challenge.UpdateDescription();
            owner.UpdateText();
            description.text = owner.challenge.description.WrapText(false, 380f);
        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            switch (message)
            {
                case "CLOSE":
                    state = State.Closing;
                    targetAlpha = 0f;
                    SteamTest.UpdateOnlineBingo();
                    break;
                case "RANDOMIZE_VARIABLE":
                    AssignChallenge(subMenu == SubMenu.Settings ? owner.challenge : null);
                    break;
                case "CHALLENGE_SETTINGS":
                    SwitchTo(SubMenu.Settings);
                    break;
                case "CHALLENGE_TYPES":
                    SwitchTo(SubMenu.Types);
                    break;
                case "UPDATE_CHALLENGE":
                    UpdateChallenge();
                    break;
                case "FOCUS":
                    FocusOn(sender as ChallengeSetting);
                    break;
                case "UNFOCUS":
                    ResetFocus();
                    break;
            }
        }

        public void AssignChallenge(Challenge ch = null)
        {
            owner.challenge = BingoHooks.GlobalBoard.RandomBingoChallenge(ch, true);
            BingoHooks.GlobalBoard.SetChallenge(owner.x, owner.y, owner.challenge, -1);
            UpdateChallenge();
            ResetSettings(owner.challenge as BingoChallenge);
        }

        private void Slide(float timeStacker)
        {
            const float EPSILON = 0.01f;
            const float LERP_SPEED = 0.1f;

            lastAlpha = currentAlpha;
            // this is where I would put my delta... If I had any !     vvv * delta);vvv
            currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, LERP_SPEED);
            closeButton.buttonBehav.greyedOut = state != State.Open;

            if (state == State.Opening && pages[0].pos.y <= EPSILON)
                state = State.Open;

            if (state == State.Closing && Math.Abs(currentAlpha - targetAlpha) < EPSILON)
            {
                title.RemoveFromContainer();
                pages[0].RemoveSubObject(description);
                for (int i = 0; i < 3; i++)
                    dividers[i].RemoveFromContainer();
                manager.StopSideProcess(this);
                state = State.Closed;
            }
        }

        private void ScrollInput()
        {
            const float EPSILON = 0.01f;
            if (manager.menuesMouseMode && mouseScrollWheelMovement != 0)
                scrollVelocity = Mathf.Sign(mouseScrollWheelMovement);

            if (scrollVelocity != 0f)
            {
                SliderSetValue(slider, Mathf.Clamp01(ValueOfSlider(slider) - scrollVelocity * 0.05f));
                scrollVelocity *= 0.8f;
            }

            if (Mathf.Abs(scrollVelocity) < EPSILON)
                scrollVelocity = 0f;
        }

        private void DrawSettings(float timeStacker)
        {
            float top = HEADER_Y - MARGIN - SETTING_HEIGHT;
            float bottom = BODY_Y + MARGIN;
            float list_height = (challengeSettings.Count - 1) * (SETTING_HEIGHT + SETTING_SPACING);
            float y = top;
            if (list_height > top - bottom)
                y = Mathf.Lerp(list_height + bottom, top, sliderF);
            foreach (ChallengeSetting setting in challengeSettings)
            {
                float overshoot = (y > bottom + (top - bottom) / 2f) ?
                        (y - top) / MARGIN :
                        (bottom - y) / MARGIN;
                setting.Alpha = Mathf.Lerp(1f, 0f, overshoot);
                setting.pos.y = y;
                y -= SETTING_HEIGHT + SETTING_SPACING;
            }
        }

        private void DrawTypes(float timeStacker)
        {
            float top = HEADER_Y - MARGIN - TYPES_HEIGHT;
            float bottom = BODY_Y + MARGIN;
            float list_height = (types.Length - 1) * (TYPES_HEIGHT + TYPES_SPACING);
            float y = top;
            if (list_height > top - bottom)
                y = Mathf.Lerp(list_height + bottom, top, sliderF);
            foreach (TypeButton type in types)
            {
                float overshoot = (y > bottom + (top - bottom) / 2f) ?
                        (y - top) / MARGIN :
                        (bottom - y) / MARGIN;
                type.MaxAlpha = Mathf.Lerp(1f, 0f, overshoot);
                type.pos.y = y;
                y -= TYPES_HEIGHT + TYPES_SPACING;
            }
        }

        private void SwitchTo(SubMenu subMenu)
        {
            this.subMenu = subMenu;
            sliderF = 1f;
            switch (subMenu)
            {
                case SubMenu.Settings:
                    selector.pos = settingsButton.pos + new Vector2(BUTTON_SIZE / 2f, BUTTON_SIZE / 2f + SELECTOR_CHEAT_OFFSET);
                    selector.lastPos = selector.pos;
                    foreach (TypeButton type in types)
                        type.MaxAlpha = 0f;
                    break;
                case SubMenu.Types:
                    selector.pos = typesButton.pos + new Vector2(BUTTON_SIZE / 2f, BUTTON_SIZE / 2f + SELECTOR_CHEAT_OFFSET);
                    selector.lastPos = selector.pos;
                    foreach (ChallengeSetting setting in challengeSettings)
                        setting.Alpha = 0f;
                    break;
            }
        }

        private void FocusOn(ChallengeSetting setting)
        {
            int g = challengeSettings.IndexOf(setting) + 1;
            for (int i = g; i < Mathf.Min(challengeSettings.Count, g + 3); i++)
                challengeSettings[i].HideField = true;
        }

        private void ResetFocus()
        {
            for (int i = 0; i < challengeSettings.Count; i++)
                challengeSettings[i].HideField = false;
        }

        public class TypeButton : ButtonTemplate
        {
            public Challenge ch;
            public FLabel text;
            private float maxAlpha;
            public float MaxAlpha
            {
                get => maxAlpha;
                set
                {
                    maxAlpha = value;
                    buttonBehav.greyedOut = value <= 0.01f;
                }
            }
            public string baseText;

            public bool IsSelected => (menu as CustomizerDialog).owner.challenge.GetType() == ch.GetType() || MouseOver;

            public TypeButton(Menu.Menu menu, MenuObject owner, Vector2 size, Challenge ch) : base(menu, owner, Vector2.zero, size)
            {
                baseText = ch.ChallengeName();
                text = new FLabel(Custom.GetFont(), baseText);
                text.SetAnchor(new Vector2(0.5f, 0f));
                text.scale = 1f;
                owner.Container.AddChild(text);
                this.ch = ch;
            }

            public override void Update()
            {
                base.Update();

                if (IsSelected)
                {
                    text.text = "> " + baseText + " <";
                }
                else text.text = baseText;
            }

            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);

                text.SetPosition(DrawPos(timeStacker) + new Vector2(200f, 0f));
                if (MouseOver)
                    text.alpha = Mathf.Clamp01(MaxAlpha - 0.5f * Mathf.Abs(Mathf.Sin(Mathf.Lerp(buttonBehav.lastSin, buttonBehav.sin, timeStacker) / 30f * Mathf.PI)));
                else
                    text.alpha = MaxAlpha;
            }

            public override void RemoveSprites()
            {
                base.RemoveSprites();
                text.RemoveFromContainer();
            }

            public override void Clicked()
            {
                base.Clicked();
                menu.PlaySound(SoundID.MENU_Add_Level);
                (menu as CustomizerDialog).AssignChallenge(ch);
            }
        }
    }
}
