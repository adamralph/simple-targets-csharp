#load "util.csx"
#load "../simple-targets-target.csx"

using System;
using System.Collections.Generic;
using System.Linq;
using static SimpleTargets;
using static SimpleTargetsUtil;

public static class SimpleTargetsTargetRunner
{
    public static void Run(IList<string> targetNames, bool dryRun, IDictionary<string, Target> targets, TextWriter output, bool color, bool skipDependencies)
    {
        if (!skipDependencies)
        {
            ValidateDependencies(targets);
        }

        ValidateTargets(targetNames, targets);

        var targetsRan = new HashSet<string>();
        foreach (var name in targetNames)
        {
            RunTarget(name, dryRun, targets, targetsRan, output, color, skipDependencies);
        }
    }

    private static void RunTarget(
        string name, bool dryRun, IDictionary<string, Target> targets, ISet<string> targetsRan, TextWriter output, bool color, bool skipDependencies)
    {
        var target = targets[name];

        if (!targetsRan.Add(name))
        {
            return;
        }

        if (!skipDependencies)
        {
            foreach (var dependency in target.Dependencies)
            {
                RunTarget(dependency, dryRun, targets, targetsRan, output, color, skipDependencies);
            }
        }

        if (target.Action != null)
        {
            output.WriteLine(StartMessage(name, color));

            if (!dryRun)
            {
                try
                {
                    target.Action.Invoke();
                }
                catch (Exception ex)
                {
                    output.WriteLine(FailureMessage(name, ex, color));
                    throw new Exception($"Target {Quote(name)} failed.", ex);
                }
            }

            output.WriteLine(SuccessMessage(name, color));
        }
    }

    private static void ValidateDependencies(IDictionary<string, Target> targets)
    {
        var missingDependencies = new SortedDictionary<string, SortedSet<string>>();

        foreach (var targetEntry in targets)
        {
            foreach (var dependencyName in targetEntry.Value.Dependencies
                .Where(dependencyName => !targets.ContainsKey(dependencyName)))
            {
                (missingDependencies.TryGetValue(dependencyName, out var set)
                        ? set
                        : missingDependencies[dependencyName] = new SortedSet<string>())
                    .Add(targetEntry.Key);
            }
        }

        if (!missingDependencies.Any())
        {
            return;
        }

        var message = $"Missing {(missingDependencies.Count > 1 ? "dependencies" : "dependency")} detected: " +
            string.Join(
                "; ",
                missingDependencies.Select(missingDependency =>
                    $@"{Quote(missingDependency.Key)}, required by {Quote(missingDependency.Value.ToList())}"));

        throw new Exception(message);
    }

    private static void ValidateTargets(IEnumerable<string> targetNames, IDictionary<string, Target> targets)
    {
        var unknownTargets = new SortedSet<string>(targetNames.Except(targets.Keys));
        if (!unknownTargets.Any())
        {
            return;
        }

        var message = $"The following target{(unknownTargets.Count() > 1 ? "s were" : " was")} not found: {Quote(unknownTargets)}.";
        throw new Exception(message);
    }
}
