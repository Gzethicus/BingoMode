using BepInEx;
using BingoMode.BingoChallenges;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using System;
using UnityEngine;

namespace BingoMode.BingoMenu
{
    internal class ChallengeSetting : PositionedMenuObject
    {
        private const float MID_ALIGN = 150f;
        private const float MARGIN = 5f;
        private const float ALPHA_THRESHOLD = 0.01f;

        private const float HEIGHT_SMALL = 24f;
        private const float HEIGHT_BIG = 30f;
        private const float DROPDOWN_SIZE = 300f; // can be arbitrarily large, it just needs to be bigger than the dropdown.

        private MenuLabel label;
        private object value;
        private MenuTabWrapper tabWrapper;
        private UIelementWrapper fieldWrapper;
        private UIconfig field;
        private SymbolButton randomize;

        private float alpha = 1f;
        public float Alpha
        {
            get => alpha;
            set
            {
                value = Mathf.Clamp01(value);
                if (alpha == value)
                    return;
                alpha = value;

                label.label.alpha = value;

                HideField = value < ALPHA_THRESHOLD;
                this.field.myContainer.alpha = value;

                if (randomize != null)
                {
                    for (int i = 0; i < 9; i++)
                        randomize.roundedRect.sprites[i].alpha = Mathf.Lerp(0f, 0.3f, value);
                    for (int i = 9; i < 17; i++)
                        randomize.roundedRect.sprites[i].alpha = value;
                    randomize.symbolSprite.alpha = value;
                    randomize.buttonBehav.greyedOut = value < ALPHA_THRESHOLD;
                }
            }
        }
        public bool HideField
        {
            set
            {
                this.field.myContainer.alpha = value ? 0f : alpha;
                this.field.greyedOut = value;
                this.field._lastGreyedOut = value;
            }
        }

        public ChallengeSetting(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, object value, float anchorX = 0f, float anchorY = 0f) : base(menu, owner, pos)
        {
            Vector2 offset = new(-anchorX * size.x, -anchorY * size.y);

            tabWrapper = new(menu, this)
            { pos = offset + new Vector2(MID_ALIGN + MARGIN / 2f, -DROPDOWN_SIZE) };
            subObjects.Add(tabWrapper);

            label = new MenuLabel(
                    menu,
                    this,
                    "",
                    offset + new Vector2(MID_ALIGN - MARGIN / 2f, size.y / 2f),
                    Vector2.zero,
                    false);
            label.label.alignment = FLabelAlignment.Right;
            subObjects.Add(label);

            ConfigurableBase conf;
            switch (value)
            {
                case SettingBox<bool> b:
                    label.text = $"{b.name}:";
                    conf = MenuModList.ModButton.RainWorldDummy.config.Bind("_ChallengeSetting", b.Value);
                    field = new OpCheckBox(
                            conf as Configurable<bool>,
                            new Vector2(0f, DROPDOWN_SIZE + (size.y - HEIGHT_SMALL) / 2f));
                    break;
                case SettingBox<int> i:
                    label.text = $"{i.name}:";
                    conf = MenuModList.ModButton.RainWorldDummy.config.Bind("_ChallengeSetting", i.Value, new ConfigAcceptableRange<int>(1, 500));
                    field = new OpUpdown(
                            true,
                            conf,
                            new Vector2(0f, DROPDOWN_SIZE + (size.y - HEIGHT_BIG) / 2f),
                            60f);
                    break;
                case SettingBox<string> s:
                    label.text = $"{s.name}:";
                    conf = MenuModList.ModButton.RainWorldDummy.config.Bind("_ChallengeSetting", s.Value);
                    field = new OpComboBox(
                            conf as Configurable<string>,
                            new Vector2(0f, DROPDOWN_SIZE + (size.y - HEIGHT_SMALL) / 2f),
                            size.x - MID_ALIGN - HEIGHT_SMALL - 1.5f * MARGIN,
                            s.listName.IsNullOrWhiteSpace() ? ["Whoops errore"] : ChallengeUtils.GetSortedCorrectListForChallenge(s.listName));
                    (field as OpComboBox).OnListOpen += Focus;
                    (field as OpComboBox).OnListClose += Unfocus;
                    randomize = new SymbolButton(
                            menu,
                            this,
                            "tinydice",
                            "RANDOMIZE",
                            offset + new Vector2(size.x - HEIGHT_SMALL, (size.y - HEIGHT_SMALL) / 2f));
                    randomize.roundedRect.size = Vector2.one * HEIGHT_SMALL;
                    subObjects.Add(randomize);
                    break;
                default:
                    throw new Exception("Invalid type for ChallengeSetting!!");
            }
            field.OnValueUpdate += Updoot;
            this.value = value;
            fieldWrapper = new UIelementWrapper(tabWrapper, field);
        }

        public override void Singal(MenuObject sender, string message)
        {
            if (message == "RANDOMIZE")
            {
                ListItem[] list = (field as OpComboBox)._itemList;
                (field as OpComboBox).value = list[UnityEngine.Random.Range(0, list.Length)].name;
            }

            base.Singal(sender, message);
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            foreach (MenuObject obj in subObjects)
            {
                obj.RemoveSprites();
                RecursiveRemoveSelectables(obj);
            }
        }

        private void Focus(UIfocusable trigger) => Singal(this, "FOCUS");

        private void Unfocus(UIfocusable trigger) => Singal(this, "UNFOCUS");

        private void Updoot(UIconfig config, string v, string oldV)
        {
            switch (value)
            {
                case SettingBox<bool>:
                    (value as SettingBox<bool>).Value = (field as OpCheckBox).GetValueBool();
                    break;
                case SettingBox<int>:
                    (value as SettingBox<int>).Value = (field as OpUpdown).GetValueInt();
                    break;
                case SettingBox<string>:
                    (value as SettingBox<string>).Value = field.value;
                    break;
            }
            Singal(this, "UPDATE_CHALLENGE");
        }
    }
}
