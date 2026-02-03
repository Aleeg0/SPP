using System.Collections;
using MyTest.exceptions;

namespace MyTest;

public static class Assert
{
    public static void AreEqual(object? expected, object? actual)
    {
        if (!Equals(expected, actual))
        {
            throw new TestFailedException($"Assert.AreEqual failed. Expected: <{expected}>. Actual: <{actual}>.");
        }
    }

    public static void AreNotEqual(object? expected, object? actual)
    {
        if (Equals(expected, actual))
        {
            throw new TestFailedException($"Assert.AreNotEqual failed. Expected: <{expected}>. Actual: <{actual}>.");
        }
    }

    public static void IsTrue(bool condition)
    {
        if (!condition)
        {
            throw new TestFailedException("Assert.IsTrue failed.");
        }
    }

    public static void IsFalse(bool condition)
    {
        if (condition)
        {
            throw new TestFailedException("Assert.IsFalse failed.");
        }
    }

    public static void Contains(object expected, IEnumerable collection)
    {
        if (collection == null)
        {
            throw new TestFailedException("Assert.Contains failure. Collection is null.");
        }

        foreach (var item in collection)
        {
            if (Equals(expected, item))
            {
                return;
            }
        }

        throw new TestFailedException($"Assert.Contains failure. Not found: <{expected}> in collection.");
    }

    public static void Contains<T>(IEnumerable<T> collection, Func<T, bool> predicate)
    {
        if (collection == null)
        {
            throw new TestFailedException("Assert.Contains failure. Collection is null.");
        }

        foreach (var item in collection)
        {
            if (predicate(item))
            {
                return;
            }
        }

        throw new TestFailedException("Assert.Contains failure. No item found matching the predicate.");
    }

    public static void DoesNotContains(object notExpected, IEnumerable collection)
    {
        if (collection == null)
        {
            throw new TestFailedException("Assert.DoesNotContains failure. Collection is null.");
        }

        foreach (var item in collection)
        {
            if (Equals(item, notExpected))
            {
                throw new TestFailedException($"Assert.DoesNotContains failure. Found: <{notExpected}> in collection.");
            }
        }
    }

    public static void DoesNotContains<T>(IEnumerable<T> collection, Func<T, bool> predicate)
    {
        if (collection == null)
        {
            throw new TestFailedException("Assert.DoesNotContains failure. Collection is null.");
        }

        foreach (var item in collection)
        {
            if (predicate(item))
            {
                throw new TestFailedException($"Assert.DoesNotContains failure. Collection is null.");
            }
        }
    }

    public static void IsNull(object actual)
    {
        if (actual != null)
        {
            throw new TestFailedException("Assert.IsNull failed. Object is NOT null.");
        }
    }

    public static void IsNotNull(object actual)
    {
        if (actual == null)
        {
            throw new TestFailedException("Assert.IsNotNull failed. Object is null.");
        }
    }

    public static void Throws<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }
        catch (Exception exception)
        {
            var thrownException = exception.GetType().Name;
            throw new TestFailedException($"Assert.Throws failed. Expected exception: {typeof(TException).Name}, but got: {thrownException}");
        }

        throw new TestFailedException(
            $"Assert.Throws failed. No exception was thrown. Expected: {typeof(TException).Name}");
    }

    public static async Task ThrowsAsync<TException>(Func<Task> action) where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException)
        {
            return;
        }
        catch (Exception exception)
        {
            var thrownException = exception.GetType().Name;
            throw new TestFailedException($"Assert.ThrowsAsync failed. Expected exception: {typeof(TException).Name}, but got: {thrownException}");
        }

        throw new TestFailedException(
            $"Assert.ThrowsAsync failed. No exception was thrown. Expected: {typeof(TException).Name}");
    }
}