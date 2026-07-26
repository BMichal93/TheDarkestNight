// =============================================================================
// THE DARKEST NIGHT — CrashDiagnostics.cs
//
// A diagnostic instrument for the recurring FIRST-TICK NATIVE crash
// (WER: Launcher.Native.exe, exception 0xc0000005 access violation,
// StackHash_f7a4, faulting module "unknown" = JIT'd managed code). Windows
// Error Reporting cannot symbolise it and ModLog.Error cannot see it: an access
// violation is a corrupted-state exception that .NET Framework does not deliver
// to ordinary managed catches, so every `try/catch` in the tick pipeline steps
// straight past it. This class exists to make the NEXT such crash leave usable
// evidence, two ways:
//
//   1. Breadcrumbs (ModLog.Breadcrumb) mark progress through the first-tick
//      settlement/kingdom pipeline. ModLog flushes each write to disk, so the
//      LAST breadcrumb in errors.log names the last phase that ran before the
//      process died — which localises the crash to a subsystem even when no
//      stack survives. Bounded (MaxBreadcrumbs) and armed only for the first few
//      campaign days, so it never spams a healthy long campaign.
//
//   2. A FirstChanceException hook logs the full managed stack of any exception
//      passing through OUR namespace (engine-internal benign exceptions are
//      filtered out to keep the noise down), plus an UnhandledException hook for
//      the fatal case if the runtime delivers it. A native AV that never becomes
//      a managed exception won't appear here — that is what the breadcrumbs are
//      for — but any managed fault that PRECEDES the crash will.
//
// Install() is idempotent and called from MainSubModule.OnGameStart. This is
// pure instrumentation: it changes no game state and can be disabled by not
// calling Install(). References no TaleWorlds types — the breadcrumb budget lives
// in ModLog (bounded to MaxBreadcrumbs), so a healthy long campaign self-limits.
// =============================================================================

using System;
using System.Runtime.ExceptionServices;

namespace TheDarkestNight
{
    internal static class CrashDiagnostics
    {
        private static bool _installed;

        public static void Install()
        {
            if (_installed) return;
            _installed = true;

            try
            {
                AppDomain.CurrentDomain.FirstChanceException += OnFirstChance;
                AppDomain.CurrentDomain.UnhandledException += OnUnhandled;
                ModLog.Breadcrumb("CrashDiagnostics installed");
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // Called from the first-tick pipeline to mark which phase is executing.
        // Self-limits via ModLog's breadcrumb budget (MaxBreadcrumbs), so it goes
        // silent once that cap is reached — well after the first-tick crash window.
        public static void MarkTick(string phase)
        {
            try
            {
                ModLog.Breadcrumb("tick: " + phase);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // Same instrument as MarkTick, for the phases that run BEFORE the first
        // daily tick (game-initialization-finished passes and new-game world
        // setup). The 2026-07-20 20:21 crash left "CrashDiagnostics installed"
        // as the last breadcrumb and no tick: line at all — i.e. the process
        // died in this earlier window, which MarkTick did not cover.
        public static void MarkPhase(string phase)
        {
            try
            {
                ModLog.Breadcrumb("phase: " + phase);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // Hourly-tick marker. Hourly handlers fire 24x per in-game day per
        // behaviour, which would burn ModLog's 600-breadcrumb budget in under
        // three days and bury the interesting lines, so this one stops on its
        // own after MaxHourlyMarks — the crash window is the first in-game
        // hours, well inside that. The 20:53 log proved the process dies after
        // FinishNewGameWorldSetup but before ANY daily handler, so the hourly
        // path is the remaining uninstrumented suspect.
        private const int MaxHourlyMarks = 120;
        private static int _hourlyMarks;

        public static void MarkHourly(string phase)
        {
            try
            {
                if (_hourlyMarks >= MaxHourlyMarks) return;
                _hourlyMarks++;
                ModLog.Breadcrumb("hour: " + phase);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void OnFirstChance(object sender, FirstChanceExceptionEventArgs e)
        {
            try
            {
                var ex = e?.Exception;
                if (ex == null) return;

                // Filter out the flood of benign engine-internal first-chance
                // exceptions: keep only ones whose type/stack passes through our
                // own code, plus genuinely severe types regardless of origin.
                bool severe = ex is AccessViolationException
                           || ex is StackOverflowException
                           || ex is System.Runtime.InteropServices.SEHException;
                if (!severe && !TouchesOurCode(ex)) return;

                // ModLog.Error de-duplicates by call site + type:message, so a
                // recurring first-chance exception is recorded once, not per frame.
                ModLog.Error(ex);
            }
            catch { /* a logger must never destabilise the handler it runs in */ }
        }

        private static void OnUnhandled(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                ModLog.Breadcrumb("UNHANDLED EXCEPTION (terminating=" + (e?.IsTerminating ?? false) + ")");
                if (e?.ExceptionObject is Exception ex) ModLog.Error(ex);
            }
            catch { /* nothing left to do if even this fails */ }
        }

        private static bool TouchesOurCode(Exception ex)
        {
            try
            {
                if (ex.Source != null && ex.Source.IndexOf("TheDarkestNight", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                string stack = ex.StackTrace;
                return stack != null && stack.IndexOf("TheDarkestNight", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch { return false; }
        }
    }
}
