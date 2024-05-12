using System;
using MODB.Api.DTOs;

namespace MODB.Api{
    public class Utilities{
        public static DBResponse<T> StopWatch<T>(Func<T> func){
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            var res = func();
            timer.Stop();
            return new DBResponse<T>(){Result = res, ProcessingTime = timer.Elapsed.ToString(@"m\:ss\.fff")};
        }

        public static DBResponse StopWatch(Action action){
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            action();
            timer.Stop();
            return new DBResponse(){ProcessingTime = timer.Elapsed.ToString(@"m\:ss\.fff")};
        }
    }
}