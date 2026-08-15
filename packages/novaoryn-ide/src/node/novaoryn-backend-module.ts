import { ContainerModule } from '@theia/core/shared/inversify';
import { ConnectionHandler, JsonRpcConnectionHandler } from '@theia/core/lib/common/messaging';
import {
    NOVAORYN_PROJECT_SERVICE_PATH,
    NovaOrynProjectService
} from '../common/novaoryn-protocol';
import { NovaOrynProjectServiceImpl } from './novaoryn-project-service';

export default new ContainerModule(bind => {
    bind(NovaOrynProjectServiceImpl).toSelf().inSingletonScope();
    bind(NovaOrynProjectService).toService(NovaOrynProjectServiceImpl);
    bind(ConnectionHandler).toDynamicValue(ctx =>
        new JsonRpcConnectionHandler(NOVAORYN_PROJECT_SERVICE_PATH, () =>
            ctx.container.get<NovaOrynProjectService>(NovaOrynProjectService)
        )
    ).inSingletonScope();
});
