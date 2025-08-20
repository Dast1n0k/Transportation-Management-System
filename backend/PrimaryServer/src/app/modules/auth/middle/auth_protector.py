import sqlite3
import jwt
from flask import request, jsonify, current_app
from functools import wraps
from app.modules.auth.core.db import get_db


def token_required(f):
    @wraps(f)
    def decorated(*args, **kwargs):
        token = request.headers.get("Authorization")

        if not token:
            return jsonify({"error": "Token is missing"}), 401

        try:
            decoded = jwt.decode(
                token, current_app.config["SECRET_KEY"], algorithms=["HS256"])
            with get_db() as conn:
                cur = conn.execute(
                    "SELECT * FROM users WHERE id = ?", (decoded["user_id"],))
                user = cur.fetchone()
            if not user:
                return jsonify({"error": "User not found"}), 401
        except jwt.ExpiredSignatureError:
            return jsonify({"error": "Token expired"}), 401
        except jwt.InvalidTokenError:
            return jsonify({"error": "Invalid token"}), 401

        return f(user, *args, **kwargs)

    return decorated
