// 
// InstrumentationData.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.Core.Instrumentation
{
	// Plain-data snapshots of the instrumentation service, used to persist the
	// instrumentation state to JSON instead of BinaryFormatter. The runtime
	// types (Counter/CounterCategory/CounterValue/TimerTrace) are getter-only
	// and not JSON-round-trippable, so they are mapped to/from these DTOs.
	class InstrumentationSnapshotDto
	{
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
		public Dictionary<string, CounterDto> Counters { get; set; } = new Dictionary<string, CounterDto> ();
		public List<CounterCategoryDto> Categories { get; set; } = new List<CounterCategoryDto> ();
	}

	class CounterCategoryDto
	{
		public string Name { get; set; } = string.Empty;
		public List<string> CounterNames { get; set; } = new List<string> ();
	}

	class CounterDto
	{
		public string Name { get; set; } = string.Empty;
		public string? Id { get; set; }
		public string? Category { get; set; }
		public bool LogMessages { get; set; }
		public bool Disposed { get; set; }
		public TimeSpan Resolution { get; set; }
		public int Count { get; set; }
		public int TotalCount { get; set; }
		public bool IsTimer { get; set; }
		public double MinSeconds { get; set; }
		public TimeSpan TotalTime { get; set; }
		public int TotalCountWithTime { get; set; }
		public TimeSpan MinTime { get; set; }
		public TimeSpan MaxTime { get; set; }
		public List<CounterValueDto> Values { get; set; } = new List<CounterValueDto> ();
	}

	class CounterValueDto
	{
		public DateTime TimeStamp { get; set; }
		public int Value { get; set; }
		public int TotalCount { get; set; }
		public string? Message { get; set; }
		public Dictionary<string, object>? Metadata { get; set; }
		public List<TimerTraceDto>? TimerTraces { get; set; }
		public TimeSpan TracesTotalTime { get; set; }
	}

	class TimerTraceDto
	{
		public DateTime Timestamp { get; set; }
		public string? Message { get; set; }
	}

	static class InstrumentationDataCodec
	{
		const string GlobalCategory = "Global";

		public static InstrumentationSnapshotDto FromService (IInstrumentationService data)
		{
			var dto = new InstrumentationSnapshotDto {
				StartTime = data.StartTime,
				EndTime = data.EndTime,
			};

			var countersByName = new Dictionary<string, Counter>();
			foreach (var c in data.GetCounters ()) {
				if (c.Name == null)
					continue;
				countersByName[c.Name] = c;
				dto.Counters[c.Name] = ToCounterDto (c);
			}

			// Categories reference counters by name so the object graph is not
			// duplicated in the serialized output.
			foreach (var cat in data.GetCategories ()) {
				var catDto = new CounterCategoryDto {
					Name = cat.Name,
				};
				foreach (var c in (cat.Counters ?? Enumerable.Empty<Counter> ())) {
					if (c.Name != null && countersByName.ContainsKey (c.Name))
						catDto.CounterNames.Add (c.Name);
				}
				dto.Categories.Add (catDto);
			}

			return dto;
		}

		public static IInstrumentationService ToService (InstrumentationSnapshotDto dto)
		{
			var categories = new List<CounterCategoryDto> (dto.Categories ?? new List<CounterCategoryDto> ());

			var catIndex = new Dictionary<string, CounterCategory> ();
			foreach (var catDto in categories) {
				if (catDto.Name == null)
					continue;
				catIndex[catDto.Name] = new CounterCategory (catDto.Name);
			}

			var counters = new Dictionary<string, Counter> ();
			foreach (var kvp in dto.Counters) {
				var c = ToCounter (kvp.Value, catIndex);
				if (c == null)
					continue;
				counters[kvp.Key] = c;
			}
			// Attach counters to their categories based on the saved membership.
			foreach (var catDto in categories) {
				if (catDto.Name == null || !catIndex.TryGetValue (catDto.Name, out var cat))
					continue;
				foreach (var counterName in catDto.CounterNames) {
					if (counters.TryGetValue (counterName, out var c))
						cat.AddCounter (c);
				}
			}

			var result = new InstrumentationServiceData (counters, catIndex.Values.ToList ()) {
				StartTime = dto.StartTime,
				EndTime = dto.EndTime,
			};
			return result;
		}

		static CounterDto ToCounterDto (Counter c)
		{
			var dto = new CounterDto {
				Name = c.Name,
				Id = c.Id,
				Category = c.Category?.Name,
				LogMessages = c.LogMessages,
				Disposed = c.Disposed,
				Resolution = c.Resolution,
				Count = c.Count,
				TotalCount = c.TotalCount,
				IsTimer = c is TimerCounter,
			};

			if (c is TimerCounter tc) {
				dto.MinSeconds = tc.MinSeconds;
				dto.TotalTime = tc.TotalTime;
				dto.TotalCountWithTime = tc.CountWithDuration;
				dto.MinTime = tc.MinTime;
				dto.MaxTime = tc.MaxTime;
			}

			foreach (var v in c.GetValues ()) {
				dto.Values.Add (ToCounterValueDto (v));
			}
			return dto;
		}

		static CounterValueDto ToCounterValueDto (CounterValue v)
		{
			var dto = new CounterValueDto {
				TimeStamp = v.TimeStamp,
				Value = v.Value,
				TotalCount = v.TotalCount,
				Message = v.Message,
				Metadata = ToDictionary (v.Metadata),
				TracesTotalTime = v.Duration,
			};

			var traces = v.GetTimerTraces ()?.ToList ();
			if (traces != null && traces.Count > 0) {
				dto.TimerTraces = traces.Select (t => new TimerTraceDto {
					Timestamp = t.Timestamp,
					Message = t.Message,
				}).ToList ();
			}
			return dto;
		}

		static Dictionary<string, object>? ToDictionary (IDictionary<string, object>? metadata)
		{
			if (metadata == null)
				return null;
			return metadata.ToDictionary (kvp => kvp.Key, kvp => kvp.Value);
		}

		static Counter? ToCounter (CounterDto dto, Dictionary<string, CounterCategory> catIndex)
		{
			CounterCategory? category = null;
			if (dto.Category != null && catIndex.TryGetValue (dto.Category, out category)) {
				// category found
			}
			// If category wasn't found, fall back to the implicit global category.
			if (category == null) {
				if (!catIndex.TryGetValue (GlobalCategory, out category)) {
					category = new CounterCategory (GlobalCategory);
					catIndex[GlobalCategory] = category;
				}
			}

			Counter c = dto.IsTimer ? (Counter) new TimerCounter (dto.Name, category)
			                        : (Counter) new Counter (dto.Name, category);
			c.RestoreState (dto.Id, dto.LogMessages, dto.Disposed, dto.Resolution, dto.Count, dto.TotalCount);

			if (c is TimerCounter tc) {
				tc.RestoreTimerState (dto.MinSeconds, dto.TotalTime, dto.TotalCountWithTime, dto.MinTime, dto.MaxTime);
			}

			var values = new List<CounterValue> ();
			foreach (var vDto in dto.Values) {
				values.Add (FromCounterValueDto (vDto));
			}
			c.RestoreValues (values);
			return c;
		}

		static CounterValue FromCounterValueDto (CounterValueDto vDto)
		{
			TimerTraceList? traces = null;
			if (vDto.TimerTraces != null && vDto.TimerTraces.Count > 0) {
				TimerTrace? first = null;
				TimerTrace? current = null;
				foreach (var tDto in vDto.TimerTraces) {
					var t = new TimerTrace {
						Timestamp = tDto.Timestamp,
						Message = tDto.Message,
					};
					if (first == null)
						first = t;
					else
						current!.Next = t;
					current = t;
				}
				traces = new TimerTraceList {
					FirstTrace = first,
					TotalTime = vDto.TracesTotalTime,
				};
				if (vDto.Metadata != null)
					traces.Metadata = vDto.Metadata;
			}
			return new CounterValue (vDto.Value, vDto.TotalCount, vDto.TimeStamp, vDto.Message, traces, vDto.Metadata);
		}
	}
}