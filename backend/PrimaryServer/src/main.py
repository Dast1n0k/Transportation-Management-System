
from flask import Flask
# from api.sentiment_routes import sentiment_bp
# from config import Config

def create_app():
    app = Flask(__name__)
    app.config.from_object(Config)
    app.register_blueprint(sentiment_bp, url_prefix = "/api/sentiment")

    return app

if __name__ == "__main__":
    app = create_app()
    app.run(debug=True)  # Debug only in development
