from __future__ import annotations

import codecs
import copy
import json
import re
import sys
from collections.abc import MutableMapping
from collections import OrderedDict
from dataclasses import dataclass
from pathlib import Path
from typing import List, Optional, Tuple


ROOT_DIR = Path(__file__).resolve().parent
VERSION_PATTERN = re.compile(r"^\d+\.\d+\.\d+(?:\.\d+)?$")
ASSEMBLY_VERSION_PATTERN = re.compile(
    r'(?m)^(?P<prefix>[ \t]*\[assembly:\s*AssemblyVersion\(")(?P<value>[^"]+)(?P<suffix>"\)\])'
)
ASSEMBLY_FILE_VERSION_PATTERN = re.compile(
    r'(?m)^(?P<prefix>[ \t]*\[assembly:\s*AssemblyFileVersion\(")(?P<value>[^"]+)(?P<suffix>"\)\])'
)


@dataclass(frozen=True)
class ProjectSpec:
    name: str
    directory_name: str
    has_manifest: bool
    is_adapter: bool = False
    is_package: bool = False


@dataclass
class TextFileState:
    path: Path
    text: str
    has_utf8_bom: bool
    newline: str
    trailing_newline: bool


@dataclass(frozen=True)
class AssemblyInfoState:
    assembly_version: str
    assembly_file_version: str


@dataclass
class ProjectState:
    spec: ProjectSpec
    manifest_file: Optional[TextFileState]
    manifest_data: Optional[MutableMapping[str, object]]
    assembly_file: TextFileState
    assembly_info: AssemblyInfoState


PROJECT_SPECS = [
    ProjectSpec("CameraSongScript", "CameraSongScript", has_manifest=True),
    ProjectSpec("CameraSongScript.CamPlus", "CameraSongScript.CamPlus", has_manifest=True, is_adapter=True),
    ProjectSpec(
        "CameraSongScript.HttpSiraStatus",
        "CameraSongScript.HttpSiraStatus",
        has_manifest=True,
        is_adapter=True,
    ),
    ProjectSpec(
        "CameraSongScript.BetterSongList",
        "CameraSongScript.BetterSongList",
        has_manifest=True,
        is_adapter=True,
    ),
    ProjectSpec("CameraSongScript.Cam2", "CameraSongScript.Cam2", has_manifest=True, is_adapter=True),
    ProjectSpec("CameraSongScript.Package", "CameraSongScript.Package", has_manifest=False, is_package=True),
]


def main() -> int:
    try:
        project_states = load_project_states()
        display_current_values(project_states)
        display_alignment_status(project_states)

        version_input = prompt_value("version", get_core_manifest_value(project_states, "version"))
        game_version_input = prompt_value("gameVersion", get_core_manifest_value(project_states, "gameVersion"))
        bsipa_input = prompt_value("BSIPA", get_core_manifest_dependency(project_states, "BSIPA"))

        if version_input:
            validate_version(version_input)

        changes_by_project, prepared_writes = build_change_plan(
            project_states,
            version_input=version_input,
            game_version_input=game_version_input,
            bsipa_input=bsipa_input,
        )

        if not prepared_writes:
            print("")
            print("変更はありません。終了します。")
            return 0

        display_change_plan(changes_by_project)

        confirmation = input("書き込む場合は ok を入力してください: ").strip().lower()
        if confirmation != "ok":
            print("中止しました。ファイルは変更していません。")
            return 0

        for file_state, new_text in prepared_writes:
            write_text_file(file_state, new_text)

        print("書き換えを完了しました。")
        return 0
    except KeyboardInterrupt:
        print("")
        print("中断しました。")
        return 130
    except Exception as exc:  # pragma: no cover - console tool error path
        print(f"エラー: {exc}", file=sys.stderr)
        return 1


def load_project_states() -> List[ProjectState]:
    project_states: List[ProjectState] = []

    for spec in PROJECT_SPECS:
        project_dir = ROOT_DIR / spec.directory_name
        assembly_path = project_dir / "Properties" / "AssemblyInfo.cs"
        assembly_file = read_text_file(assembly_path)
        assembly_info = parse_assembly_info(assembly_file)

        manifest_file: Optional[TextFileState] = None
        manifest_data: Optional[MutableMapping[str, object]] = None

        if spec.has_manifest:
            manifest_path = project_dir / "manifest.json"
            manifest_file = read_text_file(manifest_path)
            manifest_data = load_manifest_data(manifest_file)
            validate_manifest_structure(spec, manifest_path, manifest_data)

        project_states.append(
            ProjectState(
                spec=spec,
                manifest_file=manifest_file,
                manifest_data=manifest_data,
                assembly_file=assembly_file,
                assembly_info=assembly_info,
            )
        )

    return project_states


def read_text_file(path: Path) -> TextFileState:
    if not path.exists():
        raise FileNotFoundError(f"ファイルが見つかりません: {path}")

    raw = path.read_bytes()
    has_utf8_bom = raw.startswith(codecs.BOM_UTF8)
    unsupported_bom = raw.startswith(codecs.BOM_UTF16_LE) or raw.startswith(codecs.BOM_UTF16_BE)
    if unsupported_bom:
        raise ValueError(f"UTF-16 BOM は未対応です: {path}")

    encoding = "utf-8-sig" if has_utf8_bom else "utf-8"
    text = raw.decode(encoding)

    if b"\r\n" in raw:
        newline = "\r\n"
    elif b"\n" in raw:
        newline = "\n"
    else:
        newline = "\r\n"

    trailing_newline = raw.endswith(b"\r\n") or raw.endswith(b"\n")

    return TextFileState(
        path=path,
        text=text,
        has_utf8_bom=has_utf8_bom,
        newline=newline,
        trailing_newline=trailing_newline,
    )


def write_text_file(file_state: TextFileState, new_text: str) -> None:
    normalized = normalize_newlines(new_text)
    normalized = normalized.replace("\n", file_state.newline)

    if file_state.trailing_newline:
        if not normalized.endswith(file_state.newline):
            normalized += file_state.newline
    else:
        normalized = normalized.rstrip("\r\n")

    encoding = "utf-8-sig" if file_state.has_utf8_bom else "utf-8"
    with file_state.path.open("w", encoding=encoding, newline="") as handle:
        handle.write(normalized)


def normalize_newlines(text: str) -> str:
    return text.replace("\r\n", "\n").replace("\r", "\n")


def parse_assembly_info(file_state: TextFileState) -> AssemblyInfoState:
    assembly_version = extract_single_value(
        ASSEMBLY_VERSION_PATTERN,
        file_state.text,
        "AssemblyVersion",
        file_state.path,
    )
    assembly_file_version = extract_single_value(
        ASSEMBLY_FILE_VERSION_PATTERN,
        file_state.text,
        "AssemblyFileVersion",
        file_state.path,
    )
    return AssemblyInfoState(
        assembly_version=assembly_version,
        assembly_file_version=assembly_file_version,
    )


def extract_single_value(pattern: re.Pattern[str], text: str, label: str, path: Path) -> str:
    matches = list(pattern.finditer(text))
    if not matches:
        raise ValueError(f"{label} が見つかりません: {path}")
    if len(matches) != 1:
        raise ValueError(f"{label} が複数見つかりました: {path}")
    return matches[0].group("value")


def replace_single_value(pattern: re.Pattern[str], text: str, new_value: str, label: str, path: Path) -> str:
    matches = list(pattern.finditer(text))
    if not matches:
        raise ValueError(f"{label} が見つかりません: {path}")
    if len(matches) != 1:
        raise ValueError(f"{label} が複数見つかりました: {path}")

    match = matches[0]
    return "".join(
        [
            text[: match.start()],
            match.group("prefix"),
            new_value,
            match.group("suffix"),
            text[match.end() :],
        ]
    )


def load_manifest_data(file_state: TextFileState) -> MutableMapping[str, object]:
    try:
        data = json.loads(file_state.text, object_pairs_hook=OrderedDict)
    except json.JSONDecodeError as exc:
        raise ValueError(f"manifest.json の読み込みに失敗しました: {file_state.path}: {exc}") from exc

    if not isinstance(data, MutableMapping):
        raise ValueError(f"manifest.json のルートがオブジェクトではありません: {file_state.path}")

    return data


def validate_manifest_structure(spec: ProjectSpec, path: Path, manifest_data: MutableMapping[str, object]) -> None:
    require_manifest_string(manifest_data, "version", path)
    require_manifest_string(manifest_data, "gameVersion", path)
    depends_on = require_manifest_object(manifest_data, "dependsOn", path)
    require_manifest_string(depends_on, "BSIPA", path)

    if spec.is_adapter:
        require_manifest_string(depends_on, "CameraSongScript", path)


def require_manifest_object(
    source: MutableMapping[str, object],
    key: str,
    path: Path,
) -> MutableMapping[str, object]:
    value = source.get(key)
    if not isinstance(value, MutableMapping):
        raise ValueError(f"{path} の {key} がオブジェクトではありません。")
    return value


def require_manifest_string(source: MutableMapping[str, object], key: str, path: Path) -> str:
    value = source.get(key)
    if not isinstance(value, str):
        raise ValueError(f"{path} の {key} が文字列ではありません。")
    return value


def display_current_values(project_states: List[ProjectState]) -> None:
    print("現在の値:")

    for state in project_states:
        print(f"[{state.spec.name}]")

        if state.manifest_data is not None:
            print(f"  manifest.version = {require_manifest_string(state.manifest_data, 'version', state.manifest_file.path)}")
            print(
                "  manifest.gameVersion = "
                f"{require_manifest_string(state.manifest_data, 'gameVersion', state.manifest_file.path)}"
            )
            depends_on = require_manifest_object(state.manifest_data, "dependsOn", state.manifest_file.path)
            print(f"  manifest.dependsOn.BSIPA = {require_manifest_string(depends_on, 'BSIPA', state.manifest_file.path)}")
            if state.spec.is_adapter:
                print(
                    "  manifest.dependsOn.CameraSongScript = "
                    f"{require_manifest_string(depends_on, 'CameraSongScript', state.manifest_file.path)}"
                )

        print(f"  AssemblyVersion = {state.assembly_info.assembly_version}")
        print(f"  AssemblyFileVersion = {state.assembly_info.assembly_file_version}")
        print(f"  BOM = {'UTF-8 BOMあり' if state.assembly_file.has_utf8_bom else 'BOMなし'}")
        if state.manifest_file is not None:
            print(f"  manifest BOM = {'UTF-8 BOMあり' if state.manifest_file.has_utf8_bom else 'BOMなし'}")
        print("")


def display_alignment_status(project_states: List[ProjectState]) -> None:
    warnings = collect_alignment_warnings(project_states)
    if not warnings:
        print("現在の値は AdapterVersionCheck の整合条件に一致しています。")
        print("")
        return

    print("現在の値には AdapterVersionCheck の整合条件との差分があります:")
    for warning in warnings:
        print(f"  - {warning}")
    print("")


def collect_alignment_warnings(project_states: List[ProjectState]) -> List[str]:
    core_state = project_states[0]
    core_manifest_version = get_manifest_value(core_state, "version")
    core_game_version = get_manifest_value(core_state, "gameVersion")
    core_bsipa = get_manifest_dependency(core_state, "BSIPA")
    core_assembly_version = core_state.assembly_info.assembly_version

    warnings: List[str] = []
    expected_dependency = f"={core_manifest_version}"

    for state in project_states:
        if not state.spec.is_adapter:
            continue

        if state.assembly_info.assembly_version != core_assembly_version:
            warnings.append(
                f"{state.spec.name} の AssemblyVersion ({state.assembly_info.assembly_version}) が "
                f"CameraSongScript ({core_assembly_version}) と一致しません。"
            )

        adapter_dependency = get_manifest_dependency(state, "CameraSongScript")
        if adapter_dependency != expected_dependency:
            warnings.append(
                f"{state.spec.name} の dependsOn.CameraSongScript ({adapter_dependency}) が "
                f"期待値 ({expected_dependency}) と一致しません。"
            )

        adapter_game_version = get_manifest_value(state, "gameVersion")
        if adapter_game_version != core_game_version:
            warnings.append(
                f"{state.spec.name} の gameVersion ({adapter_game_version}) が "
                f"CameraSongScript ({core_game_version}) と一致しません。"
            )

        adapter_bsipa = get_manifest_dependency(state, "BSIPA")
        if adapter_bsipa != core_bsipa:
            warnings.append(
                f"{state.spec.name} の dependsOn.BSIPA ({adapter_bsipa}) が "
                f"CameraSongScript ({core_bsipa}) と一致しません。"
            )

    return warnings


def prompt_value(label: str, current_value: str) -> str:
    return input(f"{label} を入力してください [{current_value}] (空欄で変更なし): ").strip()


def validate_version(value: str) -> None:
    if not VERSION_PATTERN.fullmatch(value):
        raise ValueError("version は major.minor.build または major.minor.build.revision の数値形式で入力してください。")


def build_change_plan(
    project_states: List[ProjectState],
    version_input: str,
    game_version_input: str,
    bsipa_input: str,
) -> Tuple["OrderedDict[str, List[str]]", List[Tuple[TextFileState, str]]]:
    changes_by_project: "OrderedDict[str, List[str]]" = OrderedDict()
    prepared_writes: List[Tuple[TextFileState, str]] = []

    for state in project_states:
        project_changes: List[str] = []
        new_manifest_text: Optional[str] = None
        new_assembly_text: Optional[str] = None

        if state.manifest_data is not None and state.manifest_file is not None:
            updated_manifest = copy.deepcopy(state.manifest_data)
            manifest_changed = False

            if version_input:
                manifest_changed |= update_manifest_value(
                    updated_manifest,
                    "version",
                    version_input,
                    project_changes,
                    "manifest.version",
                    state.manifest_file.path,
                )

                if state.spec.is_adapter:
                    expected_dependency = f"={version_input}"
                    depends_on = require_manifest_object(updated_manifest, "dependsOn", state.manifest_file.path)
                    manifest_changed |= update_manifest_value(
                        depends_on,
                        "CameraSongScript",
                        expected_dependency,
                        project_changes,
                        "manifest.dependsOn.CameraSongScript",
                        state.manifest_file.path,
                    )

            if game_version_input:
                manifest_changed |= update_manifest_value(
                    updated_manifest,
                    "gameVersion",
                    game_version_input,
                    project_changes,
                    "manifest.gameVersion",
                    state.manifest_file.path,
                )

            if bsipa_input:
                depends_on = require_manifest_object(updated_manifest, "dependsOn", state.manifest_file.path)
                manifest_changed |= update_manifest_value(
                    depends_on,
                    "BSIPA",
                    bsipa_input,
                    project_changes,
                    "manifest.dependsOn.BSIPA",
                    state.manifest_file.path,
                )

            if manifest_changed:
                new_manifest_text = json.dumps(updated_manifest, indent=2, ensure_ascii=False)

        if version_input:
            target_version = normalize_package_version(version_input) if state.spec.is_package else version_input
            new_assembly_text, assembly_changes = update_assembly_versions(
                state.assembly_file,
                state.assembly_info,
                target_version,
            )
            project_changes.extend(assembly_changes)

        if project_changes:
            changes_by_project[state.spec.name] = project_changes

        if new_manifest_text is not None:
            prepared_writes.append((state.manifest_file, new_manifest_text))

        if new_assembly_text is not None:
            prepared_writes.append((state.assembly_file, new_assembly_text))

    return changes_by_project, prepared_writes


def update_manifest_value(
    manifest: MutableMapping[str, object],
    key: str,
    new_value: str,
    project_changes: List[str],
    label: str,
    path: Path,
) -> bool:
    old_value = require_manifest_string(manifest, key, path)
    if old_value == new_value:
        return False

    manifest[key] = new_value
    project_changes.append(f"{label}: {old_value} -> {new_value}")
    return True


def update_assembly_versions(
    assembly_file: TextFileState,
    assembly_info: AssemblyInfoState,
    target_version: str,
) -> Tuple[Optional[str], List[str]]:
    assembly_changes: List[str] = []
    new_text = assembly_file.text
    changed = False

    if assembly_info.assembly_version != target_version:
        new_text = replace_single_value(
            ASSEMBLY_VERSION_PATTERN,
            new_text,
            target_version,
            "AssemblyVersion",
            assembly_file.path,
        )
        assembly_changes.append(f"AssemblyVersion: {assembly_info.assembly_version} -> {target_version}")
        changed = True

    if assembly_info.assembly_file_version != target_version:
        new_text = replace_single_value(
            ASSEMBLY_FILE_VERSION_PATTERN,
            new_text,
            target_version,
            "AssemblyFileVersion",
            assembly_file.path,
        )
        assembly_changes.append(f"AssemblyFileVersion: {assembly_info.assembly_file_version} -> {target_version}")
        changed = True

    return (new_text if changed else None), assembly_changes


def normalize_package_version(version_value: str) -> str:
    parts = version_value.split(".")
    if len(parts) == 3:
        return f"{version_value}.0"
    return version_value


def display_change_plan(changes_by_project: "OrderedDict[str, List[str]]") -> None:
    print("")
    print("変更予定:")
    for project_name, changes in changes_by_project.items():
        print(f"[{project_name}]")
        for change in changes:
            print(f"  {change}")
        print("")


def get_core_manifest_value(project_states: List[ProjectState], key: str) -> str:
    return get_manifest_value(project_states[0], key)


def get_core_manifest_dependency(project_states: List[ProjectState], key: str) -> str:
    return get_manifest_dependency(project_states[0], key)


def get_manifest_value(project_state: ProjectState, key: str) -> str:
    if project_state.manifest_data is None or project_state.manifest_file is None:
        raise ValueError(f"manifest.json を持たないプロジェクトです: {project_state.spec.name}")
    return require_manifest_string(project_state.manifest_data, key, project_state.manifest_file.path)


def get_manifest_dependency(project_state: ProjectState, dependency_name: str) -> str:
    if project_state.manifest_data is None or project_state.manifest_file is None:
        raise ValueError(f"manifest.json を持たないプロジェクトです: {project_state.spec.name}")
    depends_on = require_manifest_object(project_state.manifest_data, "dependsOn", project_state.manifest_file.path)
    return require_manifest_string(depends_on, dependency_name, project_state.manifest_file.path)


if __name__ == "__main__":
    raise SystemExit(main())
