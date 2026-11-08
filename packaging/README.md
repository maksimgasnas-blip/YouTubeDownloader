# Packaging notes for the standalone yt-dlp executable

The prebuilt `yt-dlp.exe` distributed by upstream is not merely the Unlicense
Python source: it is a PyInstaller executable with third-party components and
is distributed under GPLv3+ according to upstream's release documentation.
See `../licenses/yt-dlp-THIRD_PARTY_LICENSES.txt` and the versioned upstream
licensing documentation.

When distributing the complete GUI bundle, review the GPLv3 requirements for
the combined executable and its Corresponding Source, as well as the terms for
the exact FFmpeg build, Deno executable, and all other included components.
The source repository URL alone may not meet every source-delivery obligation.

This repo's simple local build script is for development/testing only. It
copies locally installed packages and does not prove their provenance, exact
source correspondence, or legal redistribution compliance. Do not upload or
publish a binary release until that review and source packaging are complete.
