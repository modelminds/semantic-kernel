﻿// Copyright (c) Microsoft. All rights reserved.

using System.Globalization;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Events;
using Xunit;

namespace SemanticKernel.UnitTests.Events;
public class FunctionInvokedEventArgsTests
{
    [Fact]
    public void ResultValuePropertyShouldBeInitializedByOriginalOne()
    {
        //Arrange
        var originalResults = new FunctionResult("fake-function-name", 36, CultureInfo.InvariantCulture);

        var sut = new FunctionInvokedEventArgs(KernelFunctionFactory.CreateFromMethod(() => { }), new KernelArguments(), originalResults);

        //Assert
        Assert.Equal(36, sut.ResultValue);
    }

    [Fact]
    public void ResultValuePropertyShouldBeUpdated()
    {
        //Arrange
        var originalResults = new FunctionResult("fake-function-name", 36, CultureInfo.InvariantCulture);

        var sut = new FunctionInvokedEventArgs(KernelFunctionFactory.CreateFromMethod(() => { }), new KernelArguments(), originalResults);

        //Act
        sut.SetResultValue(72);

        //Assert
        Assert.Equal(72, sut.ResultValue);
    }
}
