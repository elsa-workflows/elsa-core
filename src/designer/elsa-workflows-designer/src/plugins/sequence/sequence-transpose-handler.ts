import 'reflect-metadata';
import {Service} from "typedi"
import {TransposeContext, TransposeHandler, UntransposeContext, UntransposedConnection} from "../../components/activities/flowchart/transpose-handler";

// An empty (noop) transposition handler for the Sequence activity to prevent connected activities to be assigned to its Activities property.
// The Sequence activity is a container that requires its own designer.
@Service()
export class SequenceTransposeHandler implements TransposeHandler {

  transpose(context: TransposeContext): boolean {
      return false;
  }

  untranspose(context: UntransposeContext): Array<UntransposedConnection> {
    return [];
  }
}
