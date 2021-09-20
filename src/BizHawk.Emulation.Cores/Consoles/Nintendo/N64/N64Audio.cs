﻿using System;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.N64.NativeApi;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	internal class N64Audio : IDisposable
	{
		/// <summary>
		/// mupen64 DLL Api
		/// </summary>
		private mupen64plusAudioApi api;

		private readonly mupen64plusApi coreAPI;

		/// <summary>
		/// Buffer for audio data
		/// </summary>
		private short[] audioBuffer = new short[0];
		private uint _samplingRate = 0;
		/// <summary>
		/// Currently active sampling rate
		/// </summary>
		public uint SamplingRate
		{
			get => _samplingRate;
			private set
			{
				_samplingRate = value;
				Resampler.ChangeRate(_samplingRate, 44100, _samplingRate, 44100);
			}
		}
		/// <summary>
		/// Resampler for audio output
		/// </summary>
		public SpeexResampler Resampler { get; private set; }
		public bool RenderSound { get; set; }

		/// <summary>
		/// Creates a N64 Audio subsystem
		/// </summary>
		public N64Audio(mupen64plusApi core)
		{
			this.api = new mupen64plusAudioApi(core);

			_samplingRate = api.GetSamplingRate();
			Resampler = new SpeexResampler((SpeexResampler.Quality)6, SamplingRate, 44100,
				SamplingRate, 44100);

			coreAPI = core;
			coreAPI.VInterrupt += DoAudioFrame;
		}

		/// <summary>
		/// Fetches the audio buffer from mupen64plus and pushes it into the
		/// Resampler for audio output
		/// </summary>
		public void DoAudioFrame()
		{
			try
			{
				uint m64pSamplingRate = api.GetSamplingRate();
				if (m64pSamplingRate != SamplingRate)
					SamplingRate = m64pSamplingRate;

				int audioBufferSize = api.GetAudioBufferSize();
				if (audioBuffer.Length < audioBufferSize)
					audioBuffer = new short[audioBufferSize];

				if (audioBufferSize > 0)
				{
					api.GetAudioBuffer(audioBuffer);
					if (RenderSound)
						Resampler.EnqueueSamples(audioBuffer, audioBufferSize / 2);
				}
			} catch
			{
				System.Diagnostics.Debug.WriteLine("!!!!!!!!!!! DoAudioFrame error");
			}

		}

		public void Dispose()
		{
			coreAPI.VInterrupt -= DoAudioFrame;
			Resampler?.Dispose();
			Resampler = null;
			api = null;
		}
	}
}
