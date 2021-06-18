using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CabTool.Dtos
{
    public class Device
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Configuration { get; set; }
        public string ClassName { get; set; }
        public IEnumerable<Container> Containers { get; set; }
        public string Label { get; set; }
    }

    public class Container
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string ParentId { get; set; }
        public List<Container> Children { get; set; }
        public List<IoItem> IoItems { get; set; }
        public string Name { get; set; }
        public string Label { get; set; }   // Use label to display the name; label will be generated if name does not exist

        // Physical description of the container

        public string Position { get; set; } //default is front, back, left, right, top
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; } //future use
        public float Width { get; set; }
        public float Height { get; set; }
        public float Length { get; set; } //use width, length when drawing a drawer as parent
    }

    public class EventArgs
    {
        public string Action { get; set; }
        public IoItem Item { get; set; }

        // Should a smaller data set be used?
        //		public string Id { get; set; }
        //		public string Value { get; set; }
        //		public string ValueType { get; set; }
    }

    public class IoItem
    {
        public string Id { get; set; }
        public string Direction { get; set; }
        public string ValueType { get; set; }
        public string ContainerId { get; set; }
        public string Type { get; set; }
        public string DeviceId { get; set; }
        public int? DeviceChannel { get; set; }
        public string ScheduleId { get; set; }
        public string Name { get; set; }
        public string Range { get; set; }
        public string Description { get; set; }
        public string Value { get; set; }
        public string Label { get; set; }
    }

	public class FingerprintData
	{
		public double Score { get; set; }   // 0 to 100 where 100 is best
		public CaptureQuality Quality { get; set; }

		public enum ResultCode
		{
			Success = 0,
			NotImplemented = 96075786,
			Failure = 96075787,
			NoData = 96075788,
			MoreData = 96075789,
			InvalidParameter = 96075796,
			InvalidDevice = 96075797,
			DeviceBusy = 96075806,
			DeviceFailure = 96075807,
			InvalidFID = 96075877,
			TooSmallArea = 96075878,
			InvalidFMD = 96075977,
			EnrollmentInProgress = 96076077,
			EnrollmentNotStarted = 96076078,
			EnrollmentNotReady = 96076079,
			EnrollmentInvalidSet = 96076080,
			VersionIncompatibility = 96076777
		}

		public enum CaptureQuality
		{
			Good = 0,
			TimedOut = 1,
			Cancelled = 2,
			NoFinger = 4,
			FakeFinger = 8,
			FingerTooLeft = 16,
			FingerTooRight = 32,
			FingerTooHigh = 64,
			FingerTooLow = 128,
			FingerOffCenter = 256,
			ScanSkewed = 512,
			ScanTooShort = 1024,
			ScanTooLong = 2048,
			ScanTooSlow = 4096,
			ScanTooFast = 8192,
			ScanWrongDirection = 16384,
			ReaderDirty = 32768,
			ReaderFailed = 65536
		}
	} // FingerprintData
}