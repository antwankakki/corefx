// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace System.Diagnostics
{
    internal static partial class ProcessManager
    {
        /// <summary>Gets the IDs of all processes on the current machine.</summary>
        public static int[] GetProcessIds()
        {
            return EnumerableHelpers.ToArray(EnumerateProcessIds());
        }

        /// <summary>Gets process infos for each process on the specified machine.</summary>
        /// <param name="machineName">The target machine.</param>
        /// <returns>An array of process infos, one per found process.</returns>
        public static ProcessInfo[] GetProcessInfos(string machineName)
        {
            ThrowIfRemoteMachine(machineName);
            int[] procIds = GetProcessIds(machineName);

            // Iterate through all process IDs to load information about each process
            var reusableReader = new ReusableTextReader();
            var processes = new List<ProcessInfo>(procIds.Length);
            foreach (int pid in procIds)
            {
                ProcessInfo pi = CreateProcessInfo(pid, reusableReader);
                if (pi != null)
                {
                    processes.Add(pi);
                }
            }

            return processes.ToArray();
        }

        /// <summary>Gets an array of module infos for the specified process.</summary>
        /// <param name="processId">The ID of the process whose modules should be enumerated.</param>
        /// <returns>The array of modules.</returns>
        internal static ModuleInfo[] GetModuleInfos(int processId)
        {
            var modules = new List<ModuleInfo>();

            // Process from the parsed maps file each entry representing a module
            foreach (Interop.procfs.ParsedMapsModule entry in Interop.procfs.ParseMapsModules(processId))
            {
                int sizeOfImage = (int)(entry.AddressRange.Value - entry.AddressRange.Key);

                // A single module may be split across multiple map entries; consolidate based on
                // the name and address ranges of sequential entries.
                if (modules.Count > 0)
                {
                    ModuleInfo mi = modules[modules.Count - 1];
                    if (mi._fileName == entry.FileName && 
                        ((long)mi._baseOfDll + mi._sizeOfImage == entry.AddressRange.Key))
                    {
                        // Merge this entry with the previous one
                        modules[modules.Count - 1]._sizeOfImage += sizeOfImage;
                        continue;
                    }
                }

                // It's not a continuation of a previous entry but a new one: add it.
                modules.Add(new ModuleInfo()
                {
                    _fileName = entry.FileName,
                    _baseName = Path.GetFileName(entry.FileName),
                    _baseOfDll = new IntPtr(entry.AddressRange.Key),
                    _sizeOfImage = sizeOfImage,
                    _entryPoint = IntPtr.Zero // unknown
                });
            }

            // Return the set of modules found
            return modules.ToArray();
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        /// <summary>
        /// Creates a ProcessInfo from the specified process ID.
        /// </summary>
        internal static ProcessInfo CreateProcessInfo(int pid, ReusableTextReader reusableReader = null)
        {
            if (reusableReader == null)
            {
                reusableReader = new ReusableTextReader();
            }

            Interop.procfs.ParsedStat stat;
            return Interop.procfs.TryReadStatFile(pid, out stat, reusableReader) ?
                CreateProcessInfo(stat, reusableReader) :
                null;
        }

        /// <summary>
        /// Creates a ProcessInfo from the data parsed from a /proc/pid/stat file and the associated tasks directory.
        /// </summary>
        internal static ProcessInfo CreateProcessInfo(Interop.procfs.ParsedStat procFsStat, ReusableTextReader reusableReader)
        {
            int pid = procFsStat.pid;

            var pi = new ProcessInfo()
            {
                ProcessId = pid,
                ProcessName = procFsStat.comm,
                BasePriority = (int)procFsStat.nice,
                VirtualBytes = (long)procFsStat.vsize,
                WorkingSet = procFsStat.rss,
                SessionId = procFsStat.session,

                // We don't currently fill in the other values.
                // A few of these could probably be filled in from getrusage,
                // but only for the current process or its children, not for
                // arbitrary other processes.
            };

            // Then read through /proc/pid/task/ to find each thread in the process...
            string tasksDir = Interop.procfs.GetTaskDirectoryPathForProcess(pid);
            foreach (string taskDir in Directory.EnumerateDirectories(tasksDir))
            {
                // ...and read its associated /proc/pid/task/tid/stat file to create a ThreadInfo
                string dirName = Path.GetFileName(taskDir);
                int tid;
                Interop.procfs.ParsedStat stat;
                if (int.TryParse(dirName, NumberStyles.Integer, CultureInfo.InvariantCulture, out tid) &&
                    Interop.procfs.TryReadStatFile(pid, tid, out stat, reusableReader))
                {
                    pi._threadInfoList.Add(new ThreadInfo()
                    {
                        _processId = pid,
                        _threadId = (ulong)tid,
                        _basePriority = pi.BasePriority,
                        _currentPriority = (int)stat.nice,
                        _startAddress = (IntPtr)stat.startstack,
                        _threadState = ProcFsStateToThreadState(stat.state),
                        _threadWaitReason = ThreadWaitReason.Unknown
                    });
                }
            }

            // Finally return what we've built up
            return pi;
        }

        // ----------------------------------
        // ---- Unix PAL layer ends here ----
        // ----------------------------------

        /// <summary>Enumerates the IDs of all processes on the current machine.</summary>
        internal static IEnumerable<int> EnumerateProcessIds()
        {
            // Parse /proc for any directory that's named with a number.  Each such
            // directory represents a process.
            foreach (string procDir in Directory.EnumerateDirectories(Interop.procfs.RootPath))
            {
                string dirName = Path.GetFileName(procDir);
                int pid;
                if (int.TryParse(dirName, NumberStyles.Integer, CultureInfo.InvariantCulture, out pid))
                {
                    Debug.Assert(pid >= 0);
                    yield return pid;
                }
            }
        }

        /// <summary>Gets a ThreadState to represent the value returned from the status field of /proc/pid/stat.</summary>
        /// <param name="c">The status field value.</param>
        /// <returns></returns>
        private static ThreadState ProcFsStateToThreadState(char c)
        {
            switch (c)
            {
                case 'R':
                    return ThreadState.Running;
                case 'S':
                case 'D':
                case 'T':
                    return ThreadState.Wait;
                case 'Z':
                    return ThreadState.Terminated;
                case 'W':
                    return ThreadState.Transition;
                default:
                    Debug.Fail("Unexpected status character");
                    return ThreadState.Unknown;
            }
        }

    }
}
