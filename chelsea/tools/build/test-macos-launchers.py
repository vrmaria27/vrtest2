"""Exercise Mac launcher failure behavior with local mock tools; not a Unity/device test."""
import os
import subprocess
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
class LaunchScriptTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='chelsea-launchers-')
        self.addCleanup(self.temp.cleanup)
        self.dir = Path(self.temp.name)
        self.env = os.environ.copy()
        self.env['PATH'] = str(self.dir)+os.pathsep+self.env['PATH']
        self.tool('uname', 'printf "Darwin\\n"')
        self.tool('uuidgen', 'echo MOCK-RUN-ID')
        self.env['CHELSEA_BUILD_DIR'] = str(self.dir/'build-output')
        self.env['CHELSEA_UNITY_EDITOR'] = str(self.dir/'missing-editor')
        self.env['CHELSEA_ADB'] = str(self.dir/'adb')
        self.env.pop('CHELSEA_DEVICE_SERIAL', None)
    def tool(self, name, body):
        path=self.dir/name
        path.write_text('#!/usr/bin/env bash\nset -e\n'+body+'\n');path.chmod(0o755)
        return path
    def run_script(self, script, *args):
        return subprocess.run(['bash',str(ROOT/'tools/build'/script),*map(str,args)],env=self.env,capture_output=True,text=True,timeout=10)
    def test_missing_editor_has_actionable_failure(self):
        r=self.run_script('build-alpha-macos.sh','--diagnostic')
        self.assertNotEqual(r.returncode,0);self.assertIn('Unity Hub',r.stderr)
    def test_explicit_diagnostic_required(self):
        self.env['CHELSEA_UNITY_EDITOR']=str(self.tool('editor','exit 0'))
        r=self.run_script('build-alpha-macos.sh')
        self.assertNotEqual(r.returncode,0);self.assertIn('--diagnostic',r.stderr)
    def test_failed_unity_exit_propagates(self):
        self.env['CHELSEA_UNITY_EDITOR']=str(self.tool('editor','exit 23'))
        self.assertEqual(self.run_script('build-alpha-macos.sh','--diagnostic').returncode,23)
    def test_old_apk_does_not_count_as_new_build(self):
        self.env['CHELSEA_UNITY_EDITOR']=str(self.tool('editor','exit 0'))
        directory=Path(self.env['CHELSEA_BUILD_DIR']);directory.mkdir()
        (directory/'Chelsea-Diagnostic-Alpha.apk').write_text('MOCK STALE NOT APK')
        r=self.run_script('build-alpha-macos.sh','--diagnostic')
        self.assertNotEqual(r.returncode,0);self.assertIn('success marker',r.stderr)
    def test_missing_apk_is_not_an_install_success(self):
        self.tool('adb','exit 0')
        r=self.run_script('install-quest-macos.sh',self.dir/'absent.apk')
        self.assertNotEqual(r.returncode,0);self.assertIn('APK missing',r.stderr)
    def test_unauthorised_device_does_not_install(self):
        self.tool('adb','if [[ "$1" == devices ]]; then printf "List of devices attached\\nquest unauthorized\\n"; else exit 91; fi')
        apk=self.dir/'test.apk';apk.write_text('MOCK ONLY NOT APK')
        r=self.run_script('install-quest-macos.sh',apk)
        self.assertNotEqual(r.returncode,0);self.assertIn('USB debugging',r.stderr)
    def test_multiple_devices_require_selection(self):
        self.tool('adb','if [[ "$1" == devices ]]; then printf "List of devices attached\\nquestA device\\nquestB device\\n"; else exit 91; fi')
        apk=self.dir/'test.apk';apk.write_text('MOCK ONLY NOT APK')
        r=self.run_script('install-quest-macos.sh',apk)
        self.assertNotEqual(r.returncode,0);self.assertIn('CHELSEA_DEVICE_SERIAL',r.stderr)
    def test_selected_authorised_device_command_sequence(self):
        self.env['CHELSEA_DEVICE_SERIAL']='Quest-MOCK'
        self.env['CHELSEA_TEST_COMMAND_LOG']=str(self.dir/'commands')
        self.tool('adb', '''printf '%s\\n' "$*" >> "$CHELSEA_TEST_COMMAND_LOG"
if [[ "$1" == devices ]]; then printf 'List of devices attached\\nQuest-MOCK device\\n';
elif [[ "$3" == get-state ]]; then echo device;
elif [[ "$3" == install ]]; then echo 'Mock install only';
elif [[ "$3" == shell ]]; then echo 'Mock launch only'; else exit 92; fi''')
        apk=self.dir/'test.apk';apk.write_text('MOCK ONLY NOT APK')
        r=self.run_script('install-quest-macos.sh',apk)
        self.assertEqual(r.returncode,0,r.stderr)
        log=Path(self.env['CHELSEA_TEST_COMMAND_LOG']).read_text()
        self.assertIn('-s Quest-MOCK install -r',log)
        self.assertIn('-p com.holopolis.chelsea.alpha',log)

if __name__=='__main__':unittest.main(verbosity=2)
