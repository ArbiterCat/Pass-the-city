"""
Генератор низкополигональных моделей «Ржавого квартала» для Unity.
Запуск:
  blender.exe -b --factory-startup --python Tools/Blender/build_models.py

Модели собираются из примитивов, объединяются в один меш с несколькими
материалами и экспортируются в Assets/_Project/Models/*.fbx.
Ориентация: Blender Z-up, передняя сторона моделей — -Y, низ — Z=0.
"""

import math
import os
import sys

import bpy
from mathutils import Vector

PROJECT = r"C:\Users\gisp1\Projects\SvoiPut"
OUT_DIR = os.path.join(PROJECT, "Assets", "_Project", "Models")
PREVIEW_DIR = os.path.join(PROJECT, "Tools", "Blender", "previews")

os.makedirs(OUT_DIR, exist_ok=True)
os.makedirs(PREVIEW_DIR, exist_ok=True)


# --------------------------------------------------------------------------- материалы

def material(name, color, metallic=0.0, roughness=0.6, emission=None, strength=0.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emission is not None:
        bsdf.inputs["Emission Color"].default_value = (*emission, 1.0)
        bsdf.inputs["Emission Strength"].default_value = strength
    mat.diffuse_color = (*color, 1.0)
    mat.metallic = metallic
    mat.roughness = roughness
    return mat


def build_materials():
    return {
        "M_Concrete": material("M_Concrete", (0.30, 0.285, 0.265), 0.05, 0.78),
        "M_ConcreteDark": material("M_ConcreteDark", (0.155, 0.15, 0.145), 0.05, 0.82),
        "M_Brick": material("M_Brick", (0.265, 0.135, 0.10), 0.05, 0.8),
        "M_Rust": material("M_Rust", (0.37, 0.16, 0.085), 0.45, 0.7),
        "M_RustDark": material("M_RustDark", (0.155, 0.07, 0.045), 0.55, 0.74),
        "M_Chrome": material("M_Chrome", (0.60, 0.63, 0.65), 0.92, 0.32),
        "M_ChromeGlow": material("M_ChromeGlow", (0.64, 0.67, 0.69), 0.85, 0.35, (0.85, 0.9, 0.95), 0.7),
        "M_Graphite": material("M_Graphite", (0.085, 0.095, 0.105), 0.55, 0.58),
        "M_Asphalt": material("M_Asphalt", (0.08, 0.085, 0.09), 0.12, 0.8),
        "M_Window": material("M_Window", (0.02, 0.028, 0.032), 0.35, 0.2),
        "M_WindowLit": material("M_WindowLit", (0.34, 0.19, 0.075), 0.1, 0.5, (1.0, 0.62, 0.3), 1.2),
        "M_Signal": material("M_Signal", (0.5, 0.2, 0.08), 0.25, 0.6, (1.0, 0.45, 0.12), 1.6),
        "M_Danger": material("M_Danger", (0.34, 0.08, 0.05), 0.3, 0.55, (1.0, 0.18, 0.06), 2.0),
        "M_Jacket": material("M_Jacket", (0.31, 0.145, 0.085), 0.15, 0.58),
        "M_JacketDark": material("M_JacketDark", (0.16, 0.075, 0.045), 0.2, 0.65),
        "M_Pants": material("M_Pants", (0.095, 0.10, 0.105), 0.15, 0.6),
        "M_Mask": material("M_Mask", (0.50, 0.48, 0.44), 0.1, 0.45),
        "M_Slab": material("M_Slab", (0.19, 0.18, 0.17), 0.35, 0.76),
    }


# --------------------------------------------------------------------------- примитивы

def _finish(ob, name, mat, apply_scale=True):
    ob.name = name
    if apply_scale:
        bpy.context.view_layer.objects.active = ob
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ob.data.materials.append(mat)
    return ob


def cube(name, loc, size, mat, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    ob = bpy.context.active_object
    ob.scale = size
    return _finish(ob, name, mat)


def cyl(name, loc, radius, depth, mat, rot=(0, 0, 0), verts=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=radius, depth=depth, location=loc, rotation=rot)
    return _finish(bpy.context.active_object, name, mat)


def cone(name, loc, r1, r2, depth, mat, rot=(0, 0, 0), verts=12):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r1, radius2=r2, depth=depth, location=loc, rotation=rot)
    return _finish(bpy.context.active_object, name, mat)


def sphere(name, loc, radius, mat, seg=12, rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=rings, radius=radius, location=loc)
    return _finish(bpy.context.active_object, name, mat)


def bar(name, a, b, thickness, mat):
    """Тонкий прямоугольный брус между двумя точками."""
    a = Vector(a)
    b = Vector(b)
    delta = b - a
    length = delta.length
    if length < 1e-5:
        return None
    ob = cube(name, (a + b) * 0.5, (thickness, thickness, length), mat)
    ob.rotation_euler = delta.to_track_quat('Z', 'Y').to_euler()
    return ob


def pipe(name, a, b, radius, mat, verts=8):
    a = Vector(a)
    b = Vector(b)
    delta = b - a
    length = delta.length
    if length < 1e-5:
        return None
    ob = cyl(name, (a + b) * 0.5, radius, length, mat, verts=verts)
    ob.rotation_euler = delta.to_track_quat('Z', 'Y').to_euler()
    return ob


# --------------------------------------------------------------------------- экспорт

def join_and_export(name):
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    bpy.ops.object.select_all(action='DESELECT')
    for ob in meshes:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.join()
    result = bpy.context.active_object
    result.name = name
    result.data.name = name

    # Нормали и чистая геометрия.
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.object.mode_set(mode='OBJECT')

    path = os.path.join(OUT_DIR, name + ".fbx")
    bpy.ops.object.select_all(action='DESELECT')
    result.select_set(True)
    bpy.context.view_layer.objects.active = result
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        object_types={'MESH'},
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        mesh_smooth_type='FACE',
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode='STRIP',
    )
    print("EXPORTED", path, "polys=", len(result.data.polygons))
    return result


def preview(name, target_height=1.0, distance=7.0):
    scene = bpy.context.scene
    bpy.ops.object.camera_add(location=(distance * 0.72, -distance, distance * 0.55))
    cam = bpy.context.object
    target = Vector((0.0, 0.0, target_height * 0.55))
    cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler()
    cam.data.lens = 55
    scene.camera = cam

    scene.render.engine = 'BLENDER_WORKBENCH'
    shading = scene.display.shading
    shading.light = 'STUDIO'
    shading.studio_light = 'paint.sl'
    shading.color_type = 'MATERIAL'
    shading.show_shadows = True
    shading.show_cavity = True
    shading.cavity_type = 'BOTH'
    scene.display.render_aa = '8'
    scene.render.resolution_x = 640
    scene.render.resolution_y = 640
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = False
    if scene.world is None:
        scene.world = bpy.data.worlds.new("World")
    scene.world.color = (0.055, 0.06, 0.065)
    scene.view_settings.view_transform = 'Standard'
    scene.render.filepath = os.path.join(PREVIEW_DIR, name + ".png")
    bpy.ops.render.render(write_still=True)
    print("PREVIEW", scene.render.filepath)


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    return build_materials()


# --------------------------------------------------------------------------- вода на крыше

def model_water_tower():
    M = reset()
    rust, dark, chrome, graphite = M["M_Rust"], M["M_RustDark"], M["M_Chrome"], M["M_Graphite"]

    for i in range(4):
        x = -1.4 if i % 2 == 0 else 1.4
        y = -1.4 if i < 2 else 1.4
        cube("Leg%d" % i, (x, 0.0 if i < 2 else 0.0, 0), (0.22, 0.22, 1), rust)  # placeholder replaced below
        bpy.context.object.location = (x, y, 2.3)
        bpy.context.object.scale = (0.22, 0.22, 4.6)
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    for z in (1.35, 3.15):
        cube("BraceX1", (0, -1.4, z), (2.9, 0.13, 0.13), dark)
        cube("BraceX2", (0, 1.4, z), (2.9, 0.13, 0.13), dark)
        cube("BraceY1", (-1.4, 0, z), (0.13, 2.9, 0.13), dark)
        cube("BraceY2", (1.4, 0, z), (0.13, 2.9, 0.13), dark)

    cyl("Tank", (0, 0, 5.75), 1.52, 2.75, rust, verts=16)
    cyl("BandLow", (0, 0, 4.62), 1.56, 0.07, chrome, verts=16)
    cyl("BandHigh", (0, 0, 6.86), 1.56, 0.07, chrome, verts=16)
    cone("TankRoof", (0, 0, 7.42), 1.62, 0.28, 0.72, dark, verts=16)
    cyl("TankNeck", (0, 0, 7.92), 0.22, 0.28, graphite, verts=8)
    cylinder_cap = cyl("TankCap", (0, 0, 8.08), 0.34, 0.06, chrome, verts=8)

    # Ржавые накладки на баке.
    for i, (x, y, z, rot) in enumerate([
        (-1.49, 0.15, 5.1, (0, math.radians(90), 0)),
        (1.45, -0.35, 6.15, (0, math.radians(90), 0)),
        (0.1, 1.48, 5.6, (math.radians(90), 0, 0)),
    ]):
        cube("RustPatch%d" % i, (x, y, z), (0.55, 0.32, 0.02) if abs(x) > 1.4 else (0.55, 0.02, 0.32), dark, rot=rot)

    # Лестница на передней стороне.
    for side in (-1, 1):
        pipe("LadderRail%d" % side, (0.35 * side, -1.62, 0.25), (0.35 * side, -1.62, 7.2), 0.045, chrome)
    for i in range(12):
        z = 0.55 + i * 0.58
        cube("Rung%d" % i, (0, -1.62, z), (0.76, 0.06, 0.055), chrome)

    # Водосточная труба.
    pipe("Downpipe", (1.55, 0.5, 6.2), (1.55, 0.5, 0.4), 0.08, dark)
    pipe("DownpipeElbow", (1.55, 0.5, 0.4), (1.9, 0.9, 0.2), 0.08, dark)

    join_and_export("WaterTower")
    preview("WaterTower", 7.5, 12.0)


# --------------------------------------------------------------------------- кондиционер

def model_ac_unit():
    M = reset()
    chrome, graphite, rust, dark = M["M_Chrome"], M["M_Graphite"], M["M_Rust"], M["M_RustDark"]

    cube("Base", (0, 0, 0.06), (1.62, 1.22, 0.12), graphite)
    cube("Body", (0, 0, 0.52), (1.5, 1.1, 0.84), chrome)
    cube("TopRim", (0, 0, 0.95), (1.56, 1.16, 0.05), graphite)
    cyl("FanHole", (0, 0, 0.975), 0.44, 0.02, graphite, verts=16)
    cyl("FanHub", (0, 0, 0.99), 0.08, 0.03, chrome, verts=8)
    for i in range(4):
        a = math.radians(i * 45)
        cube("Grille%d" % i, (0, 0, 0.995), (0.82, 0.06, 0.02), chrome, rot=(0, 0, a))
    cube("RustPatch", (0.28, -0.56, 0.55), (0.58, 0.02, 0.30), rust)
    cube("RustPatch2", (-0.52, 0.56, 0.36), (0.30, 0.02, 0.18), dark)
    pipe("Drain", (0.48, -0.42, 0.12), (0.48, -0.9, 0.02), 0.05, dark)
    cube("DataPlate", (-0.52, -0.561, 0.78), (0.22, 0.01, 0.13), dark)
    cube("VentL", (-0.62, -0.561, 0.5), (0.16, 0.02, 0.36), graphite)

    join_and_export("ACUnit")
    preview("ACUnit", 1.0, 3.2)


# --------------------------------------------------------------------------- ящик

def model_crate():
    M = reset()
    rust, dark, graphite, chrome = M["M_Rust"], M["M_RustDark"], M["M_Graphite"], M["M_Chrome"]

    cube("Body", (0, 0, 0.48), (0.96, 0.96, 0.92), rust)
    cube("Lid", (0, 0, 0.97), (1.02, 1.02, 0.07), dark)
    cube("BandX", (0, 0, 0.5), (1.0, 0.12, 0.94), graphite)
    cube("BandY", (0, 0, 0.55), (0.12, 1.0, 0.94), graphite)
    for sx in (-1, 1):
        for sy in (-1, 1):
            cube("Corner", (sx * 0.455, sy * 0.455, 0.5), (0.09, 0.09, 0.96), chrome)
    for sy in (-1, 1):
        ob = cube("XBrace", (0, sy * 0.49, 0.48), (1.0, 0.03, 0.08), dark, rot=(0, math.radians(38), 0))
        ob.location = (0, sy * 0.49, 0.5)
    cube("Label", (0.22, -0.485, 0.55), (0.26, 0.015, 0.16), dark)

    join_and_export("Crate")
    preview("Crate", 1.0, 2.8)


# --------------------------------------------------------------------------- бочка

def model_barrel():
    M = reset()
    rust, dark, chrome, signal = M["M_Rust"], M["M_RustDark"], M["M_Chrome"], M["M_Signal"]

    cyl("Body", (0, 0, 0.45), 0.31, 0.9, rust, verts=14)
    cyl("BandLow", (0, 0, 0.2), 0.325, 0.05, chrome, verts=14)
    cyl("BandHigh", (0, 0, 0.7), 0.325, 0.05, chrome, verts=14)
    cyl("Rim", (0, 0, 0.9), 0.3, 0.045, dark, verts=14)
    cyl("Top", (0, 0, 0.925), 0.28, 0.02, rust, verts=14)
    cube("Label", (0, -0.3, 0.5), (0.22, 0.02, 0.18), signal)
    cube("RustPatch", (0.25, -0.18, 0.42), (0.16, 0.02, 0.26), dark)
    cyl("Bung", (0.12, 0.0, 0.94), 0.055, 0.03, chrome, verts=8)

    join_and_export("Barrel")
    preview("Barrel", 1.0, 2.4)


# --------------------------------------------------------------------------- фонарь

def model_street_lamp():
    M = reset()
    rust, dark, chrome, signal = M["M_Rust"], M["M_RustDark"], M["M_Chrome"], M["M_Signal"]

    cube("Base", (0, 0, 0.1), (0.5, 0.5, 0.2), dark)
    cyl("Post", (0, 0, 3.1), 0.09, 6.0, dark, verts=10)
    cyl("Collar", (0, 0, 0.45), 0.13, 0.28, rust, verts=10)
    cube("Arm", (0.9, 0, 6.0), (1.8, 0.12, 0.12), dark)
    bar("Brace", (0, 0, 5.3), (1.45, 0, 5.95), 0.06, dark)
    cube("Head", (1.92, 0, 5.86), (0.7, 0.36, 0.18), rust)
    cube("Glow", (1.92, 0, 5.76), (0.54, 0.24, 0.03), signal)
    cube("Hood", (1.92, 0, 5.98), (0.78, 0.4, 0.06), dark)
    cube("RustPatch", (0.04, -0.1, 2.2), (0.18, 0.02, 0.5), rust)
    cyl("Finial", (0, 0, 6.2), 0.13, 0.18, chrome, verts=8)

    join_and_export("StreetLamp")
    preview("StreetLamp", 6.2, 10.0)


# --------------------------------------------------------------------------- патрульная машина

def model_patrol_car():
    M = reset()
    rust, dark, chrome, graphite = M["M_Rust"], M["M_RustDark"], M["M_Chrome"], M["M_Graphite"]
    window, signal, danger = M["M_Window"], M["M_Signal"], M["M_Danger"]

    cube("Lower", (0, 0, 0.78), (4.6, 2.0, 0.66), rust)
    cube("Cabin", (-0.2, 0, 1.38), (2.35, 1.78, 0.64), rust)
    cube("Hood", (1.76, 0, 1.0), (1.28, 1.9, 0.3), rust)
    cube("Trunk", (-1.95, 0, 1.0), (0.92, 1.9, 0.28), rust)
    cube("Roof", (-0.2, 0, 1.72), (2.2, 1.7, 0.08), dark)
    cube("BumperF", (2.33, 0, 0.62), (0.16, 2.02, 0.26), chrome)
    cube("BumperB", (-2.33, 0, 0.62), (0.16, 2.02, 0.26), chrome)
    cube("WindowL", (-0.2, -0.9, 1.42), (1.85, 0.04, 0.44), window)
    cube("WindowR", (-0.2, 0.9, 1.42), (1.85, 0.04, 0.44), window)
    cube("Windshield", (1.0, 0, 1.42), (0.06, 1.58, 0.46), window)
    cube("RearGlass", (-1.4, 0, 1.42), (0.06, 1.58, 0.44), window)
    cube("PillarL", (0.55, -0.9, 1.42), (0.1, 0.06, 0.5), rust)
    cube("PillarR", (0.55, 0.9, 1.42), (0.1, 0.06, 0.5), rust)
    cube("DoorLine", (-0.34, -1.01, 0.85), (0.04, 0.02, 0.46), dark)
    cube("RustDoor", (0.55, -1.01, 0.86), (0.8, 0.02, 0.5), dark)
    cube("SideStripe", (0.4, -1.02, 1.18), (1.5, 0.02, 0.08), graphite)

    for i in range(4):
        x = -1.5 if i % 2 == 0 else 1.5
        y = -1.02 if i < 2 else 1.02
        wheel = cyl("Wheel%d" % i, (x, y, 0.36), 0.34, 0.18, graphite, rot=(math.radians(90), 0, 0), verts=14)
        cyl("Hub%d" % i, (x, y, 0.36), 0.16, 0.19, chrome, rot=(math.radians(90), 0, 0), verts=10)

    # Фары и задние огни: перед +X.
    for y in (-0.62, 0.62):
        cube("Headlamp", (2.34, y, 0.9), (0.05, 0.32, 0.2), signal)
        cube("TailLamp", (-2.34, y, 0.9), (0.05, 0.28, 0.18), danger)
    cube("LightBar", (-0.2, 0, 1.8), (0.8, 0.9, 0.14), chrome)
    cube("LightBarBlue", (-0.2, 0, 1.88), (0.55, 0.25, 0.05), signal)
    cube("Antenna", (-1.5, 0.75, 2.05), (0.04, 0.04, 0.7), chrome)

    join_and_export("PatrolCar")
    preview("PatrolCar", 2.0, 7.0)


# --------------------------------------------------------------------------- последний поезд

def model_train_car():
    M = reset()
    rust, dark, chrome = M["M_Rust"], M["M_RustDark"], M["M_Chrome"]
    graphite, window, lit = M["M_Graphite"], M["M_Window"], M["M_WindowLit"]
    glow, signal = M["M_ChromeGlow"], M["M_Signal"]

    cube("Underframe", (0, 0, 0.2), (18.0, 3.0, 0.38), graphite)
    cube("Floor", (0, 0, 0.46), (17.8, 2.8, 0.1), M["M_ConcreteDark"])

    # Дальняя (задняя) длинная стенка.
    cube("RearWall", (0, 1.46, 1.55), (18.0, 0.14, 2.3), dark)
    cube("RearBand", (0, 1.52, 2.95), (18.05, 0.14, 0.5), rust)
    # Передняя стенка с проёмом двери.
    cube("FrontLeft", (-5.1, -1.46, 1.55), (7.8, 0.14, 2.3), dark)
    cube("FrontRight", (5.1, -1.46, 1.55), (7.8, 0.14, 2.3), dark)
    cube("FrontTop", (0, -1.46, 2.95), (18.0, 0.14, 0.5), rust)
    cube("FrontBottom", (0, -1.46, 0.62), (2.4, 0.14, 0.3), dark)

    # Торцы вагона.
    cube("EndL", (-9.0, 0, 1.55), (0.14, 3.0, 2.3), dark)
    cube("EndR", (9.0, 0, 1.55), (0.14, 3.0, 2.3), dark)
    cube("EndBandL", (-9.05, 0, 2.95), (0.14, 3.05, 0.5), rust)
    cube("EndBandR", (9.05, 0, 2.95), (0.14, 3.05, 0.5), rust)

    cube("Roof", (0, 0, 3.28), (18.3, 3.1, 0.24), graphite)
    cube("RoofEdgeL", (0, -1.49, 3.2), (18.1, 0.2, 0.1), rust)
    cube("RoofEdgeR", (0, 1.49, 3.2), (18.1, 0.2, 0.1), rust)

    # Открытая дверь.
    for x in (-1.2, 1.2):
        cube("DoorPost", (x, -1.56, 1.55), (0.12, 0.14, 2.4), chrome)
        cube("DoorGlow", (x * 1.05, -1.63, 1.55), (0.05, 0.03, 2.2), glow)
    cube("DoorTop", (0, -1.56, 2.72), (2.5, 0.14, 0.12), chrome)
    cube("DoorGlowTop", (0, -1.63, 2.79), (2.4, 0.03, 0.05), glow)
    cube("Threshold", (0, -1.56, 0.48), (2.4, 0.26, 0.1), chrome)
    door_l = cube("DoorLeafL", (-1.7, -1.64, 1.55), (0.9, 0.07, 2.3), rust)
    door_l.rotation_euler = (0, 0, math.radians(-14))
    door_r = cube("DoorLeafR", (1.7, -1.64, 1.55), (0.9, 0.07, 2.3), rust)
    door_r.rotation_euler = (0, 0, math.radians(14))

    # Окна вдоль передней стенки.
    for i in range(8):
        x = -7.3 + i * 2.05
        if abs(x) < 1.8:
            continue
        cube("Window%d" % i, (x, -1.545, 2.95), (1.25, 0.04, 0.28), lit)
    cube("NumberPlate", (-6.4, -1.555, 1.52), (1.5, 0.04, 0.66), graphite)

    # Интерьер.
    cube("InteriorBack", (0, 1.34, 1.6), (17.4, 0.1, 2.4), graphite)
    cube("InteriorCeil", (0, 0, 2.76), (17.4, 2.7, 0.08), graphite)
    cube("InteriorFloor", (0, 0, 0.53), (17.4, 2.7, 0.05), dark)
    cube("BenchL", (-0.85, 0.72, 0.9), (1.3, 0.5, 0.42), rust)
    cube("BenchR", (0.85, 0.72, 0.9), (1.3, 0.5, 0.42), rust)
    cube("InteriorGlow", (0, 0.2, 2.68), (2.0, 0.5, 0.06), glow)

    # Тележки и колёса.
    for b in (-1, 1):
        bx = b * 5.9
        cube("Bogie", (bx, 0, 0.1), (2.7, 2.2, 0.45), graphite)
        for i in range(4):
            wx = bx + (-0.8 if i % 2 == 0 else 0.8)
            wy = -1.08 if i < 2 else 1.08
            cyl("Wheel", (wx, wy, -0.06), 0.3, 0.16, rust, rot=(math.radians(90), 0, 0), verts=12)

    # Крышное оборудование.
    cube("Vent", (4.2, 0, 3.48), (1.8, 1.0, 0.22), dark)
    cube("Vent2", (-3.4, 0.4, 3.48), (1.2, 0.8, 0.2), graphite)
    cube("PantoBase", (0.8, 0, 3.48), (1.6, 1.3, 0.16), graphite)
    p1 = cube("PantoA", (0.35, 0, 3.95), (0.07, 0.07, 1.1), chrome)
    p1.rotation_euler = (0, math.radians(28), 0)
    p2 = cube("PantoB", (1.25, 0, 3.95), (0.07, 0.07, 1.1), chrome)
    p2.rotation_euler = (0, math.radians(-28), 0)
    cube("PantoBar", (0.8, 0, 4.48), (1.9, 0.14, 0.07), chrome)

    join_and_export("TrainCar")
    preview("TrainCar", 3.0, 19.0)


# --------------------------------------------------------------------------- выживший

def model_survivor():
    M = reset()
    jacket, jacket_dark = M["M_Jacket"], M["M_JacketDark"]
    pants, mask, graphite = M["M_Pants"], M["M_Mask"], M["M_Graphite"]
    chrome, signal = M["M_Chrome"], M["M_Signal"]
    rust_dark = M["M_RustDark"]

    # Ноги и ботинки.
    for side in (-1, 1):
        x = side * 0.17
        cube("Boot", (x, -0.04, 0.09), (0.21, 0.36, 0.18), pants)
        cyl("Leg", (x, 0, 0.44), 0.1, 0.36, pants, verts=10)
        cyl("Knee", (x, 0, 0.63), 0.11, 0.12, jacket_dark, verts=10)
    cube("Hips", (0, 0, 0.74), (0.44, 0.31, 0.22), pants)

    # Куртка и ремни.
    cube("Jacket", (0, 0, 1.08), (0.56, 0.35, 0.6), jacket)
    cube("JacketHem", (0, 0, 0.81), (0.58, 0.37, 0.16), jacket_dark)
    for side in (-1, 1):
        cube("Strap", (side * 0.16, -0.185, 1.1), (0.07, 0.03, 0.56), jacket_dark)
    cube("Zipper", (0, -0.186, 1.1), (0.035, 0.02, 0.52), graphite)
    cube("Scarf", (0, 0, 1.38), (0.38, 0.34, 0.13), jacket_dark)
    cube("ScarfTail", (0.08, 0.2, 1.24), (0.16, 0.05, 0.34), jacket_dark)

    # Руки.
    for side in (-1, 1):
        x = side * 0.39
        arm = cyl("Arm", (x, 0, 1.05), 0.08, 0.55, jacket, verts=10)
        arm.rotation_euler = (0, math.radians(side * -10), 0)
        cube("Glove", (x + side * 0.04, -0.04, 0.75), (0.15, 0.19, 0.17), pants)
        cube("Cuff", (x, 0, 0.8), (0.18, 0.18, 0.08), jacket_dark)

    # Голова, капюшон, маска, очки (лицо смотрит в -Y).
    sphere("Head", (0, 0, 1.62), 0.215, mask, seg=14, rings=10)
    sphere("Hood", (0, 0.035, 1.66), 0.255, jacket_dark, seg=14, rings=10)
    cube("Face", (0, -0.195, 1.6), (0.27, 0.065, 0.19), mask)
    cube("Goggles", (0, -0.225, 1.67), (0.31, 0.055, 0.085), graphite)
    for side in (-1, 1):
        cube("Lens", (side * 0.075, -0.253, 1.67), (0.105, 0.018, 0.06), chrome)
    cyl("Filter", (0, -0.24, 1.53), 0.055, 0.06, graphite, rot=(math.radians(90), 0, 0), verts=10)
    cube("HoodBrim", (0, -0.14, 1.78), (0.36, 0.16, 0.05), jacket_dark)

    # Рюкзак и снаряжение (сзади +Y).
    cube("Backpack", (0, 0.29, 1.1), (0.40, 0.19, 0.5), pants)
    cube("PackStrap", (0, 0.39, 1.2), (0.34, 0.03, 0.07), jacket_dark)
    bedroll = cyl("Bedroll", (0, 0.3, 1.38), 0.13, 0.28, rust_dark, rot=(0, math.radians(90), 0), verts=10)
    cube("Radio", (0.13, 0.39, 1.32), (0.05, 0.05, 0.28), graphite)
    pipe("Antenna", (0.13, 0.39, 1.46), (0.13, 0.39, 1.74), 0.012, chrome, verts=6)

    # Нагрудный фонарь.
    cube("ChestLamp", (0.17, -0.19, 1.2), (0.10, 0.05, 0.11), graphite)
    cube("ChestLens", (0.17, -0.22, 1.2), (0.065, 0.018, 0.07), signal)

    join_and_export("Survivor")
    preview("Survivor", 1.9, 3.8)


# --------------------------------------------------------------------------- станок

def model_machine():
    M = reset()
    graphite, rust, dark = M["M_Graphite"], M["M_Rust"], M["M_RustDark"]
    chrome, signal = M["M_Chrome"], M["M_Signal"]
    window = M["M_Window"]

    cube("Base", (0, 0, 0.16), (2.5, 2.1, 0.32), graphite)
    cube("Body", (0, 0, 1.35), (2.1, 1.75, 2.1), graphite)
    cube("Top", (0, 0, 2.48), (2.2, 1.85, 0.2), dark)
    cube("Panel", (-0.55, -0.89, 1.6), (0.86, 0.05, 0.72), dark)
    cube("Screen", (-0.55, -0.92, 1.75), (0.44, 0.02, 0.28), window)
    cube("Button1", (-0.82, -0.93, 1.3), (0.1, 0.03, 0.1), signal)
    cube("Button2", (-0.62, -0.93, 1.3), (0.1, 0.03, 0.1), chrome)
    cube("HazardStripe", (0.64, -0.9, 0.65), (0.62, 0.03, 0.18), signal)
    cube("Vent", (0.55, -0.89, 1.95), (0.62, 0.04, 0.42), dark)
    for i in range(5):
        cube("VentBar", (0.55, -0.915, 1.8 + i * 0.075), (0.52, 0.02, 0.025), graphite)
    cyl("Tank", (0.95, 0.55, 1.5), 0.36, 1.7, rust, verts=12)
    cyl("TankTop", (0.95, 0.55, 2.38), 0.42, 0.16, dark, verts=12)
    pipe("PipeUp", (0.95, 0.55, 2.45), (0.95, 0.55, 3.35), 0.09, chrome, verts=8)
    pipe("PipeBend", (0.95, 0.55, 3.35), (0.1, 0.55, 3.35), 0.09, chrome, verts=8)
    pipe("PipeDown", (0.1, 0.55, 3.35), (0.1, 0.55, 2.6), 0.09, chrome, verts=8)
    cube("RustPatch", (-0.7, 0.88, 0.9), (0.7, 0.02, 0.5), rust)
    cube("Cable", (1.05, -0.5, 0.4), (0.06, 0.6, 0.06), chrome)

    join_and_export("IndustrialMachine")
    preview("IndustrialMachine", 3.0, 6.0)


# --------------------------------------------------------------------------- main

def main():
    models = [
        model_water_tower,
        model_ac_unit,
        model_crate,
        model_barrel,
        model_street_lamp,
        model_patrol_car,
        model_train_car,
        model_survivor,
        model_machine,
    ]
    for build in models:
        print("BUILDING", build.__name__)
        build()
    print("ALL MODELS DONE")


if __name__ == "__main__":
    main()
