# Repository Moved as of 21 Feb 2026

The repository has now been absorbed into the main [SIPSorcery](https://github.com/sipsorcery-org/sipsorcery) repository. Any iusses or pull requests should be raised there.

# VP8.Net

This project is an attempt to port the [WebM](https://www.webmproject.org/) VP8 video codec to C#.

The motivation for the project is to provide a .NET video codec that does not require any native libraries for use in the sipsorcery real-time communications library.

**As of Feb 2026:**

 - VP8 decoder works but is very slow. A [demo program](https://github.com/sipsorcery-org/sipsorcery/tree/master/examples/WebRTCExamples/WebRTCClientVP8Net) is available.
 - VP8 encoder is now implemented with basic keyframe encoding support. See [ENCODER_IMPLEMENTATION.md](ENCODER_IMPLEMENTATION.md) for details.
