
using System;
using System.Collections.Generic;

namespace cowsins2D
{
    [Serializable]
    public class DialogJsonRoot
    {
        public List<DialogFormJson> intro = new List<DialogFormJson>();
        public List<DialogFormJson> tutorial = new List<DialogFormJson>();
        public List<DialogFormJson> end = new List<DialogFormJson>();
    }

    [Serializable]
    public class DialogFormJson
    {
        public string type;
        public string textCaption;
        public string textName;
        public string spritePortrait;
        public string spriteDescript;
        public List<ChoiceOptionJson> choices = new List<ChoiceOptionJson>();
    }

    [Serializable]
    public class ChoiceOptionJson
    {
        public string text;
        public string afterText;
        public string aftersprite;
        public string sprite;
        public string answerType;
        public List<DialogFormJson> afterDialogs = new List<DialogFormJson>();
    }
}
