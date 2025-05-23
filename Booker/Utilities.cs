﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Booker
{
    public static class Utilities
    {
        /// <summary>
        /// Renumber the children of a folder to progress by 10s.
        /// </summary>
        /// <param name="path">Folder to renumber</param>
        /// <param name="recursive">If true, then also renumber folders within this one.</param>
        public static void RenumberFolder(string path, bool recursive = false) {
            var children = Directory.GetFileSystemEntries(path)
                .Where(p => Path.GetExtension(p) == SiteGenerator.SourceExtension || Directory.Exists(p)).ToArray();
            Array.Sort(children);
            if (Path.GetFileName(children[0]) != "0.md") {
                // Something wrong...
                return;
            }

            var big = children.Length >= 10;
            for (var i = 1; i < children.Length; i++) {
                string childPath = children[i];
                var name = Path.GetFileName(childPath);
                var realName = name.Substring(name.IndexOf(' ') + 1);
                var sequenceNumber = big ? $"{i * 10:D3}" : $"{i * 10:D2}";
                var newPath = Path.Combine(path, $"{sequenceNumber} {realName}");
                if (Directory.Exists(childPath)) {
                    if (newPath != childPath)
                        Directory.Move(childPath, newPath);

                    if (recursive)
                        RenumberFolder(newPath, recursive);
                } else if (newPath != childPath)
                    File.Move(childPath, newPath);
            }
        }
    }
}
