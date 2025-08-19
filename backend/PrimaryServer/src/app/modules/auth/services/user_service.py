import sqlite3
from werkzeug.security import generate_password_hash
from app.modules.auth.core.db import get_db
from app.modules.auth.utils.validators import validate_user_data, validate_role, validate_password


def get_all_users():
    """Получает список всех пользователей"""
    with get_db() as conn:
        cur = conn.execute(
            "SELECT id, username, role, created_at FROM users ORDER BY username"
        )
        users = [dict(row) for row in cur.fetchall()]

    return users


def create_user_by_admin(username, password, role):
    """Создает нового пользователя (только админы)"""
    # Валидация
    validation_error = validate_user_data(username, password)
    if validation_error:
        return None, validation_error

    role_error = validate_role(role)
    if role_error:
        return None, role_error

    hashed_password = generate_password_hash(password)

    with get_db() as conn:
        try:
            conn.execute(
                "INSERT INTO users (username, password, role) VALUES (?, ?, ?)",
                (username, hashed_password, role),
            )
            conn.commit()
            return True, None
        except sqlite3.IntegrityError:
            return None, {
                'error': 'Username already exists',
                'code': 'USERNAME_EXISTS'
            }


def delete_user_by_id(user_id, current_user):
    """Удаляет пользователя по ID"""
    # Админ не может удалить самого себя
    if str(current_user['id']) == str(user_id):
        return None, {
            'error': 'Cannot delete yourself',
            'code': 'SELF_DELETE_FORBIDDEN'
        }

    with get_db() as conn:
        # Проверяем существование пользователя
        cur = conn.execute("SELECT * FROM users WHERE id = ?", (user_id,))
        user_to_delete = cur.fetchone()

        if not user_to_delete:
            return None, {
                'error': 'User not found',
                'code': 'USER_NOT_FOUND'
            }

        # Проверяем, что не удаляем последнего админа
        if user_to_delete["role"] == "admin":
            cur = conn.execute(
                "SELECT COUNT(*) as count FROM users WHERE role = 'admin'")
            admin_count = cur.fetchone()["count"]

            if admin_count <= 1:
                return None, {
                    'error': 'Cannot delete the last admin',
                    'code': 'LAST_ADMIN_DELETE_FORBIDDEN'
                }

        # Удаляем пользователя
        conn.execute("DELETE FROM users WHERE id = ?", (user_id,))
        conn.commit()

        return dict(user_to_delete), None


def change_user_role_by_id(user_id, new_role, current_user):
    """Изменяет роль пользователя"""
    # Валидация роли
    role_error = validate_role(new_role)
    if role_error:
        return None, role_error

    # Админ не может изменить свою роль
    if str(current_user['id']) == str(user_id):
        return None, {
            'error': 'Cannot change your own role',
            'code': 'SELF_ROLE_CHANGE_FORBIDDEN'
        }

    with get_db() as conn:
        # Проверяем существование пользователя
        cur = conn.execute("SELECT * FROM users WHERE id = ?", (user_id,))
        user_to_update = cur.fetchone()

        if not user_to_update:
            return None, {
                'error': 'User not found',
                'code': 'USER_NOT_FOUND'
            }

        # Если понижаем админа, проверяем что не последний
        if user_to_update["role"] == "admin" and new_role != "admin":
            cur = conn.execute(
                "SELECT COUNT(*) as count FROM users WHERE role = 'admin'")
            admin_count = cur.fetchone()["count"]

            if admin_count <= 1:
                return None, {
                    'error': 'Cannot demote the last admin',
                    'code': 'LAST_ADMIN_DEMOTE_FORBIDDEN'
                }

        # Обновляем роль
        conn.execute("UPDATE users SET role = ? WHERE id = ?",
                     (new_role, user_id))
        conn.commit()

        return dict(user_to_update), None


def update_user_password(user_id, new_password):
    """Обновляет пароль пользователя"""
    password_error = validate_password(new_password)
    if password_error:
        return False, password_error

    hashed_password = generate_password_hash(new_password)

    with get_db() as conn:
        conn.execute(
            "UPDATE users SET password = ? WHERE id = ?",
            (hashed_password, user_id)
        )
        conn.commit()

    return True, None
