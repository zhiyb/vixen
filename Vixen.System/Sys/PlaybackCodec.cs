using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Serialization;

namespace Vixen.Sys
{	
	public class PlaybackCodec
	{
		public static bool initialised = false;

		// Basics
		[DllImport("codec.dll")]
		public static extern void codec_init();

		[DllImport("codec.dll")]
		public static extern int codec_version();

		[DllImport("codec.dll", CharSet = CharSet.Ansi)]
		public static extern IntPtr find_codec(string codec_name);

		[DllImport("codec.dll")]
		public static extern IntPtr codec_alloc();

		[DllImport("codec.dll")]
		public static extern void codec_free(IntPtr data);

		// Video encoder
		[DllImport("codec.dll", CharSet = CharSet.Ansi)]
		public static extern int encode_open_output(IntPtr data,
			string file, string comment);

		[DllImport("codec.dll")]
		public static extern int encode_add_audio_stream_copy(IntPtr data, IntPtr dec_ac);

		[DllImport("codec.dll", CharSet = CharSet.Ansi)]
		public static extern int encode_add_video_stream(IntPtr data, IntPtr vcodec,
			string pix_fmt_name, int resolution, int channels);

		[DllImport("codec.dll", CharSet = CharSet.Ansi)]
		public static extern IntPtr encode_write_header(IntPtr data, string file);

		[DllImport("codec.dll")]
		public static extern int encode_write_packet_or_frame(IntPtr data,
			IntPtr pkt, IntPtr frame);

		[DllImport("codec.dll")]
		public static extern void encode_close(IntPtr data);

		// Video decoder
		[DllImport("codec.dll")]
		public static extern int decode_open_input(IntPtr data,
			string file, ref IntPtr comment,
			out int gota, out int gotv);

		[DllImport("codec.dll")]
		public static extern IntPtr decode_context(IntPtr data, int video);

		[DllImport("codec.dll")]
		public static extern IntPtr decode_read_packet(IntPtr data,
			out int got, out int video);

		[DllImport("codec.dll")]
		public static extern IntPtr decode_audio_frame(IntPtr data, IntPtr pkt);

        [DllImport("codec.dll")]
        public static extern int decode_audio_sample_rate(IntPtr data);

        [DllImport("codec.dll")]
        public static extern int decode_audio_frame_samples(IntPtr frame);

		[DllImport("codec.dll")]
		public static extern IntPtr decode_video_frame(IntPtr data, IntPtr pkt);

		[DllImport("codec.dll")]
		public static extern void decode_free_packet(IntPtr pkt);

		[DllImport("codec.dll")]
		public static extern void decode_close(IntPtr data);

		// Channel data
		[DllImport("codec.dll")]
		public static extern IntPtr encode_channels(IntPtr data,
			byte[] frame, int channels);

		[DllImport("codec.dll")]
		public static extern IntPtr decode_channels(IntPtr data, IntPtr frame,
			int channels);

		// FMOD audio engine
		[DllImport("codec.dll")]
		public static extern uint fmod_init(IntPtr data);

		[DllImport("codec.dll")]
		public static extern uint fmod_version(IntPtr data);

		[DllImport("codec.dll")]
		public static extern uint fmod_create_stream(IntPtr data, IntPtr dec, uint sync);

		[DllImport("codec.dll")]
		public static extern uint fmod_play(IntPtr data);

		[DllImport("codec.dll")]
		public static extern void fmod_close(IntPtr data);

		[DllImport("codec.dll")]
		public static extern void fmod_queue_frame(IntPtr data, IntPtr frame);

		[DllImport("codec.dll")]
		public static extern uint fmod_update(IntPtr data);

		[DllImport("codec.dll")]
		public static extern int fmod_is_playing(IntPtr data);
	}
}
