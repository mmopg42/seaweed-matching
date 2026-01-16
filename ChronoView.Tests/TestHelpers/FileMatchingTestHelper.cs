using ChronoView.Core.FileMatching;
using ChronoView.Core.GroupIdGeneration;
using Microsoft.Extensions.Logging;

namespace ChronoView.Tests.TestHelpers;

public static class FileMatchingTestHelper
{
    public static FileGroupMatcherService CreateMatcher(ILogger<FileGroupMatcherService>? logger = null, bool lineSpecificGroupId = false)
    {
        IGroupIdGenerator idGen = lineSpecificGroupId ? new LineBasedGroupIdGenerator() : new GlobalGroupIdGenerator();
        return new FileGroupMatcherService(idGen, logger);
    }
}


