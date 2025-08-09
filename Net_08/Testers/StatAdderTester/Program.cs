using System;
using System.Collections.Generic;
using SDAQFramework.MathUtilities; 

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Stat adder!");
//
//
//
var data = new List<double> { 
11.01336444, 11.20521411, 11.91966328, 11.17737785, 
11.6375078, 11.46133676, 11.38820653, 11.00109269, 
11.25624459, 11.2506453,
10.71405245, 10.90016623, 10.20576205, 10.84366742,
10.64633749, 10.08165948, 10.92755803, 10.2330572,
10.83146173, 10.07614908 };


var statAdder = new StatAccumulator(samplesToSkip: 10 ,autoStart: true );

foreach (var value in data) {
    statAdder.AddValue(value);    
}

statAdder.Stop();
Console.WriteLine("Stat adder stopped!");
var result = statAdder.Result;
Console.WriteLine($"Stat adder done!\n\n{result}");
