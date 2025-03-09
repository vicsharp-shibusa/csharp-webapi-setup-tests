﻿namespace Alpha.Common;

public interface IOperationContext
{
    Guid OperationId { get; }
    DateTimeOffset Start { get; }
}
