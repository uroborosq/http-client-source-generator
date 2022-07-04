from datetime import datetime
from fastapi import FastAPI, Query, Body
from models import Request, Manager
from service import ManagersService, RequestService

app = FastAPI()
managerService = ManagersService()
requestsService = RequestService()


@app.get("/requests/get")
async def read_root() -> list[Request]:
    return requestsService.get_requests()


@app.post("/requests/create")
async def add_request(client: str = Query(''), manager: Manager = Body(None), date_begin: datetime = Body(datetime.today())) -> Request:
    return requestsService.add_request(client, manager, date_begin)


@app.post("/managers/create")
async def add_manager(name: str = Query('')) -> Manager:
    return managerService.add_manager(name)


@app.get("/managers/get")
async def get_managers() -> list[Manager]:
    return managerService.get_manger()
