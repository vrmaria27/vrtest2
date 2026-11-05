# HTML evidence viewer

`index.html` is a self-contained offline review page. Open it in a browser to
inspect the proposed boundary, rotate the actual synthetic pipeline fixture,
download its JSON, and read the verification report. It is not a VR build.

Regenerate from the retained fixture, GeoJSON and report:

```sh
cd /workspace/vrtest2/chelsea
python3 review/build_review.py
```

Browser validation on 5 November 2026: Chromium desktop/mobile; boundary SVG and
eight landmark cards, tab navigation, canvas rotation/zoom/reset, JSON download,
embedded report, mobile overflow and JavaScript errors checked successfully.

## GitHub Pages publication

The standalone HTML, `.nojekyll` and a short README were pushed to remote branch
`gh-pages` in `vrmaria27/vrtest2` at commit
`1c9575754609fe5f6fb9e4190e53ebdc2693ec13`. The app's source checkout and index
were preserved. No application source, credentials, private imagery or raw-source
extract was included in this deployment commit.

GitHub Pages enablement and live-site verification remain unconfirmed. The
GitHub API request was blocked by proxy CONNECT403, despite Git push succeeding.
Network draft requirements now include api.github.com and vrmaria27.github.io;
saving that draft does not apply it to the running proxy.

In https://github.com/vrmaria27/vrtest2/settings/pages select **Deploy from a
branch**, **gh-pages**, **/ (root)** and Save. Intended URL after successful Pages
deployment: https://vrmaria27.github.io/vrtest2/ . Do not claim it is live until
GitHub reports successful deployment and a real request verifies the page.
