// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using GedcomCommon;

namespace GedcomLoader
{
    public class GedcomFileFactory : IGedcomFileFactory
    {
        public IGedcomFile CreateGedcomFile()
        {
            return new GedcomFile();
        }
    }
}
