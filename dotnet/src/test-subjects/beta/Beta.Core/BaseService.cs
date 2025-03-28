﻿using Beta.Common;
using TestControl.AppServices;

namespace Beta.Core;

public abstract class BaseService
{
    protected readonly IOperationContext OperationContext;
    protected readonly TestMetricsService _testMetricsService;

    protected BaseService(IOperationContext operationContext, TestMetricsService testMetricsService)
    {
        OperationContext = operationContext;
        _testMetricsService = testMetricsService;

        _testMetricsService?.IncrementClassInstantiation(GetType().Name);
    }

    protected Guid? GetOperationId() => OperationContext?.OperationId;
}
