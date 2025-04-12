using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;

namespace BenchmarkTests.Config;

public class BenchmarkConfig : ManualConfig
{
	public BenchmarkConfig()
	{
		AddJob(Job.Default.WithId(".NET 9").WithRuntime(CoreRuntime.Core90));

		SummaryStyle = SummaryStyle.Default
			.WithTimeUnit(TimeUnit.Nanosecond)
			.WithRatioStyle(RatioStyle.Percentage);
	}
}
