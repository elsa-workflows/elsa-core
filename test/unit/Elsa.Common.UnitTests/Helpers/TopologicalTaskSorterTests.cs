using Elsa.Common.Helpers;
using System.Threading.Tasks;

namespace Elsa.Common.UnitTests.Helpers;

public class TopologicalTaskSorterTests
{
    // Task with single dependency
    [TaskDependency(typeof(TaskA))]
    private class TaskWithDependency : ITask
    {
        public Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    // Task with multiple dependencies
    [TaskDependency(typeof(TaskA))]
    [TaskDependency(typeof(TaskB))]
    private class TaskWithMultipleDependencies : ITask
    {
        public Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    // Chain of dependencies: TaskChainC -> TaskChainB -> TaskChainA
    private class TaskChainA : ITask
    {
        public Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [TaskDependency(typeof(TaskChainA))]
    private class TaskChainB : ITask
    {
        public Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [TaskDependency(typeof(TaskChainB))]
    private class TaskChainC : ITask
    {
        public Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    // Circular dependency: TaskCircular1 -> TaskCircular2 -> TaskCircular1
    [TaskDependency(typeof(TaskCircular2))]
    private class TaskCircular1 : ITask
    {
        public Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [TaskDependency(typeof(TaskCircular1))]
    private class TaskCircular2 : ITask
    {
        public Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    // Self-referencing circular dependency
    [TaskDependency(typeof(TaskSelfCircular))]
    private class TaskSelfCircular : ITask
    {
        public Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [Test]
    public async Task Sort_WithNoTasks_ReturnsEmptyList()
    {
        // Arrange
        var tasks = Array.Empty<ITask>();

        // Act
        var result = TopologicalTaskSorter.Sort(tasks);

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task Sort_WithSingleTask_ReturnsSingleTask()
    {
        // Arrange
        var tasks = new ITask[] { new TaskA() };

        // Act
        var result = TopologicalTaskSorter.Sort(tasks);

        // Assert
        await Assert.That(result).HasSingleItem();
        await Assert.That(result[0]).IsOfType(typeof(TaskA));
    }

    [Test]
    public async Task Sort_WithNoDependencies_ReturnsAllTasks()
    {
        // Arrange
        var taskA = new TaskA();
        var taskB = new TaskB();
        var taskC = new TaskC();
        var tasks = new ITask[] { taskA, taskB, taskC };

        // Act
        var result = TopologicalTaskSorter.Sort(tasks);

        // Assert
        await Assert.That(result.Count).IsEqualTo(3);
        await Assert.That(result).Contains(taskA);
        await Assert.That(result).Contains(taskB);
        await Assert.That(result).Contains(taskC);
    }

    [Test]
    public async Task Sort_WithSingleDependency_OrdersCorrectly()
    {
        // Arrange
        var taskA = new TaskA();
        var taskWithDependency = new TaskWithDependency();
        var tasks = new ITask[] { taskWithDependency, taskA };

        // Act
        var result = TopologicalTaskSorter.Sort(tasks);

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0]).IsOfType(typeof(TaskA));
        await Assert.That(result[1]).IsOfType(typeof(TaskWithDependency));
    }

    [Test]
    public async Task Sort_WithMultipleDependencies_OrdersCorrectly()
    {
        // Arrange
        var taskA = new TaskA();
        var taskB = new TaskB();
        var taskWithMultipleDeps = new TaskWithMultipleDependencies();
        var tasks = new ITask[] { taskWithMultipleDeps, taskB, taskA };

        // Act
        var result = TopologicalTaskSorter.Sort(tasks);

        // Assert
        await Assert.That(result.Count).IsEqualTo(3);
        var resultList = result.ToList();
        var dependentIndex = resultList.IndexOf(taskWithMultipleDeps);
        var taskAIndex = resultList.IndexOf(taskA);
        var taskBIndex = resultList.IndexOf(taskB);

        // Both dependencies should come before the dependent task
        await Assert.That(taskAIndex < dependentIndex).IsTrue();
        await Assert.That(taskBIndex < dependentIndex).IsTrue();
    }

    [Test]
    public async Task Sort_WithChainedDependencies_OrdersCorrectly()
    {
        // Arrange
        var taskChainA = new TaskChainA();
        var taskChainB = new TaskChainB();
        var taskChainC = new TaskChainC();
        var tasks = new ITask[] { taskChainC, taskChainA, taskChainB };

        // Act
        var result = TopologicalTaskSorter.Sort(tasks);

        // Assert
        await Assert.That(result.Count).IsEqualTo(3);
        await Assert.That(result[0]).IsOfType(typeof(TaskChainA));
        await Assert.That(result[1]).IsOfType(typeof(TaskChainB));
        await Assert.That(result[2]).IsOfType(typeof(TaskChainC));
    }

    [Test]
    public async Task Sort_WithCircularDependency_ThrowsInvalidOperationException()
    {
        // Arrange
        var task1 = new TaskCircular1();
        var task2 = new TaskCircular2();
        var tasks = new ITask[] { task1, task2 };

        // Act & Assert
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => TopologicalTaskSorter.Sort(tasks));
        await Assert.That(exception.Message).Contains("Circular dependency detected");
    }

    [Test]
    public async Task Sort_WithSelfCircularDependency_ThrowsInvalidOperationException()
    {
        // Arrange
        var task = new TaskSelfCircular();
        var tasks = new ITask[] { task };

        // Act & Assert
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => TopologicalTaskSorter.Sort(tasks));
        await Assert.That(exception.Message).Contains("Circular dependency detected");
    }

    [Test]
    public async Task Sort_WithMultipleInstancesOfSameType_PreservesAllInstances()
    {
        // Arrange
        var taskA1 = new TaskA();
        var taskA2 = new TaskA();
        var taskA3 = new TaskA();
        var tasks = new ITask[] { taskA1, taskA2, taskA3 };

        // Act
        var result = TopologicalTaskSorter.Sort(tasks);

        // Assert
        await Assert.That(result.Count).IsEqualTo(3);
        await Assert.That(result).Contains(taskA1);
        await Assert.That(result).Contains(taskA2);
        await Assert.That(result).Contains(taskA3);
    }

    [Test]
    public async Task Sort_WithMultipleInstancesOfSameTypeWithDependencies_OrdersCorrectly()
    {
        // Arrange
        var taskA1 = new TaskA();
        var taskA2 = new TaskA();
        var taskWithDep1 = new TaskWithDependency();
        var taskWithDep2 = new TaskWithDependency();
        var tasks = new ITask[] { taskWithDep1, taskA1, taskWithDep2, taskA2 };

        // Act
        var result = TopologicalTaskSorter.Sort(tasks);

        // Assert
        await Assert.That(result.Count).IsEqualTo(4);

        // All TaskA instances should come before TaskWithDependency instances
        var resultList = result.ToList();
        var firstTaskAIndex = resultList.IndexOf(taskA1);
        var secondTaskAIndex = resultList.IndexOf(taskA2);
        var firstTaskWithDepIndex = resultList.IndexOf(taskWithDep1);
        var secondTaskWithDepIndex = resultList.IndexOf(taskWithDep2);

        await Assert.That(firstTaskAIndex < firstTaskWithDepIndex).IsTrue();
        await Assert.That(firstTaskAIndex < secondTaskWithDepIndex).IsTrue();
        await Assert.That(secondTaskAIndex < firstTaskWithDepIndex).IsTrue();
        await Assert.That(secondTaskAIndex < secondTaskWithDepIndex).IsTrue();
    }

    [Test]
    public async Task Sort_WithMixedDependenciesAndIndependentTasks_OrdersCorrectly()
    {
        // Arrange
        var taskA = new TaskA();
        var taskB = new TaskB();
        var taskC = new TaskC();
        var taskWithDep = new TaskWithDependency();
        var tasks = new ITask[] { taskC, taskWithDep, taskB, taskA };

        // Act
        var result = TopologicalTaskSorter.Sort(tasks);

        // Assert
        await Assert.That(result.Count).IsEqualTo(4);

        // TaskA must come before TaskWithDependency
        var resultList = result.ToList();
        var taskAIndex = resultList.IndexOf(taskA);
        var taskWithDepIndex = resultList.IndexOf(taskWithDep);
        await Assert.That(taskAIndex < taskWithDepIndex).IsTrue();

        // TaskB and TaskC can be anywhere (no dependencies)
        await Assert.That(result).Contains(taskB);
        await Assert.That(result).Contains(taskC);
    }
    
    // Test task classes without dependencies
    private class TaskA : ITask
    {
        public Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private class TaskB : ITask
    {
        public Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private class TaskC : ITask
    {
        public Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
