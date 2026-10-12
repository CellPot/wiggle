using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mono.Menu
{
    // Full-screen dimmed overlay with a title, an optional detail line and a column of buttons.
    public class MenuScreen
    {
        public VisualElement Root { get; }
        public Label Detail { get; }

        public MenuScreen(VisualElement parent, string title, string detail = "")
        {
            Root = new VisualElement();
            Root.style.position = Position.Absolute;
            Root.style.left = 0;
            Root.style.right = 0;
            Root.style.top = 0;
            Root.style.bottom = 0;
            Root.style.alignItems = Align.Center;
            Root.style.justifyContent = Justify.Center;
            Root.style.backgroundColor = new Color(0f, 0f, 0f, 0.6f);
            Root.style.display = DisplayStyle.None;

            var titleLabel = new Label(title);
            titleLabel.style.fontSize = 56;
            titleLabel.style.color = Color.white;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            Root.Add(titleLabel);

            Detail = new Label(detail);
            Detail.style.fontSize = 24;
            Detail.style.color = new Color(0.85f, 0.85f, 0.85f);
            Detail.style.marginTop = 10;
            Detail.style.marginBottom = 20;
            Root.Add(Detail);

            parent.Add(Root);
        }

        public void AddButton(string text, Action onClick)
        {
            var button = new Button(onClick) { text = text };
            button.style.width = 220;
            button.style.height = 48;
            button.style.fontSize = 22;
            button.style.marginTop = 8;
            Root.Add(button);
        }

        public void SetVisible(bool visible)
        {
            Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
