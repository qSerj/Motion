# core/geometry.py
import numpy as np

def calculate_angle_3d(a: list, b: list, c: list) -> float:
    """
    Вычисляет угол между тремя точками в 3D пространстве.
    a, b, c - координаты [x, y, z]
    b - центральная вершина (сустав)
    """
    a = np.array(a)  # Например, Плечо
    b = np.array(b)  # Например, Локоть
    c = np.array(c)  # Например, Запястье

    # Создаем векторы
    ba = a - b
    bc = c - b

    # Вычисляем косинус угла через скалярное произведение
    cosine_angle = np.dot(ba, bc) / (np.linalg.norm(ba) * np.linalg.norm(bc))

    # Защита от ошибок float (чтобы не вышло за пределы -1..1)
    angle = np.arccos(np.clip(cosine_angle, -1.0, 1.0))

    # Перевод в градусы
    return np.degrees(angle)

def calculate_distance(p1: list, p2: list) -> float:
    """
    Считает Евклидово расстояние между двумя точками (в условных единицах).
    p1, p2 - координаты [x, y, z] или [x, y]
    """
    a = np.array(p1)
    b = np.array(p2)
    return np.linalg.norm(a - b)