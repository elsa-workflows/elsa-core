# MassTransit Alterations Background Runner

This module provides an implementation that processes alteration plans in the background using a MassTransit dispatcher and consumer.
This implementation is more resilient than the in-memory queue implementation provided by **Elsa.Alterations.BackgroundRunner** because it uses a message broker to persist messages.