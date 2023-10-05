// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Text;

namespace Gedcom7
{
    public class GedcomTagInfo
    {
        public string Tag { get; private set; }
        public string Uri { get; private set; }
        public GedcomTagInfo(string tag, string uri = null)
        {
            this.Tag = tag;
            this.Uri = uri;
        }
        static Dictionary<string, GedcomTagInfo> _tagInfos = new Dictionary<string, GedcomTagInfo>();
        public static GedcomTagInfo GetTagInfo(string tag)
        {
            if (!_tagInfos.ContainsKey(tag)) {
                _tagInfos[tag] = new GedcomTagInfo(tag);
            }
            return _tagInfos[tag];
        }
    }
}
