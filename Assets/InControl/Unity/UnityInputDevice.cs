using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace InControl
{
	public class UnityInputDevice : InputDevice
	{
		public int JoystickId { get; private set; }
		public UnityInputDeviceProfile Profile { get; protected set; }


		public UnityInputDevice( UnityInputDeviceProfile profile, int joystickId = 0 )
			: base( profile.Name, profile.AnalogMappings.Length, profile.ButtonMappings.Length )
		{
			Profile = profile;

			Meta = Profile.Meta;

			foreach (var analogMapping in Profile.AnalogMappings)
			{
				AddAnalogControl( analogMapping.Target, analogMapping.Handle );
			}

			foreach (var buttonMapping in Profile.ButtonMappings)
			{
				AddButtonControl( buttonMapping.Target, buttonMapping.Handle );
			}

			JoystickId = joystickId;

			if (joystickId != 0)
			{
				Meta += " [id: " + joystickId + "]";
			}
		}


		public override void Update( float updateTime, float deltaTime )
		{
			if (Profile == null)
			{
				return;
			}

			var analogMappingCount = Profile.AnalogMappings.Length;
			for (int i = 0; i < analogMappingCount; i++)
			{
				var analogMapping = Profile.AnalogMappings[i];
				var unityValue = analogMapping.Source.GetValue( this );

				if (!analogMapping.Raw)
				{
					if (analogMapping.TargetRangeIsNotComplete && unityValue == 0.0f && Analogs[i].UpdateTime == 0.0f)
					{
						// Ignore initial input stream for triggers, because they report 
						// zero incorrectly until the value changes for the first time.
						// Example: wired Xbox controller on Mac.
						continue;
					}

					var smoothValue = SmoothAnalogValue( unityValue, Analogs[i].LastValue, deltaTime );
					var mappedValue = analogMapping.MapValue( smoothValue );
					Analogs[i].UpdateWithValue( mappedValue, updateTime );
				}
				else
				{
					Analogs[i].UpdateWithValue( unityValue, updateTime );
				}
			}

			var buttonMappingCount = Profile.ButtonMappings.Length;
			for (int i = 0; i < buttonMappingCount; i++)
			{
				var buttonMapping = Profile.ButtonMappings[i];
				var buttonState = buttonMapping.Source.GetState( this );
				
				Buttons[i].UpdateWithState( buttonState, updateTime );
			}
		}


		float SmoothAnalogValue( float thisValue, float lastValue, float deltaTime )
		{
			if (Profile.IsJoystick)
			{
				// Apply dead zone.
				thisValue = Mathf.InverseLerp( Profile.DeadZone, 1.0f, Mathf.Abs( thisValue ) ) * Mathf.Sign( thisValue );

				// Apply sensitivity (how quickly the value adapts to changes).
				float maxDelta = deltaTime * Profile.Sensitivity * 100.0f;

				// Move faster towards zero when changing direction.
				if (Mathf.Sign( lastValue ) != Mathf.Sign( thisValue )) 
				{
					maxDelta *= 2;
				}

				return Mathf.Clamp( lastValue + Mathf.Clamp( thisValue - lastValue, -maxDelta, maxDelta ), -1.0f, 1.0f );
			}
			else
			{
				return thisValue;
			}
		}


		public bool IsConfiguredWith( UnityInputDeviceProfile deviceProfile, int joystickId )
		{
			return Profile == deviceProfile && JoystickId == joystickId;
		}
	}
}
